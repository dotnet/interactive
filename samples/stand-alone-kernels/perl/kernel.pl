# Copyright (c) .NET Foundation and contributors. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.

# This is designed to be a stand-alone, out-of-process kernel, primarily used to ensure proper proxy
# handling.  It requires a local `perl.exe` interpreter be available and can be used by executing
# the following in a notebook:
#
#     #!connect stdio --kernel-name perl --command perl.exe kernel.pl

use JSON;
use Capture::Tiny qw(capture);
use Try::Tiny;

$|++; # autoflush

# run with `perl.exe kernel.pl test` to ensure the value serialization works as expected
if (length(@ARGV) > 0 && $ARGV[0] eq "test") {
    test();
} else {
    main(@ARGV);
}

sub test {
    $scalarNumber = 4;
    $scalarString = "four";
    @array = (1, 2, 3);
    $arrayRef = [4, 5, 6];
    %hash = ("theAnswer" => 42, "pi" => 3.14159);
    $hashRef = {"theAnswer" => 42, "pi" => 3.14159};

    @names = ("scalarNumber", "scalarString", "array", "arrayRef", "hash", "hashRef");
    @mimeTypes = ("text/plain", "application/json");
    foreach my $mimeType (@mimeTypes) {
        print "$mimeType:\n";
        foreach my $name (@names) {
            print "    $name: " . getStringRepresentationOfValueName($name, $mimeType) . "\n";
        }
    }

    print "type names\n";
    foreach my $name (@names) {
        print "    $name: " . getTypeNameOfValueName($name) . "\n";
    }
}

sub main {
    my $kernelHost = "pid-$$";

    # the line "#!connect stdio ..." auto-appends these arguments
    for (my $i = 0; $i < $#_; $i++) {
        my $arg = $_[$i];
        if ($arg eq "--kernel-host") {
            $i++;
            $kernelHost = $_[$i];
        }
    }

    my $kernelUri = "kernel://$kernelHost/";

    %suppressedValues = {};
    foreach my $valueName ( keys %main:: ) {
        if (!$suppressedValues{$valueName}) {
            $suppressedValues{$valueName} = 1;
        }
    }

    $kernelInfo = {
        "localName" => "perl",
        "languageName" => "perl",
        "languageVersion" => "$^V",
        "displayName" => "Perl $^V",
        "uri" => $kernelUri,
        "supportedKernelCommands" => [
            { "name" => "RequestKernelInfo" },
            { "name" => "RequestValue" },
            { "name" => "RequestValueInfos" },
            { "name" => "SendValue" },
            { "name" => "SubmitCode" },
            { "name" => "Quit" }
        ],
        "supportedDirectives" => []
    };

    publish({
        "eventType" => "KernelReady",
        "event" => {
            "kernelInfos" => $kernelInfo
        },
        "command" => undef,
        "routingSlip" => [
            $kernelUri
        ]
    });

    while (<STDIN>) {
        chomp;
        try {
            $envelope = decode_json($_);
            $envelopeRoutingSlip = $envelope->{"routingSlip"};
            push(@$envelopeRoutingSlip, $kernelUri . "?tag=arrived");
            $commandType = $envelope->{'commandType'};
            if ($commandType) {
                $token = $envelope->{'token'};
                $command = $envelope->{'command'};
                $succeeded = false;
                if ($commandType eq "Quit") {
                    #
                    #                                                      Quit
                    #
                    return;
                } elsif ($commandType eq "RequestKernelInfo") {
                    #
                    #                                         RequestKernelInfo
                    #
                    publish({
                        "eventType" => "KernelInfoProduced",
                        "event" => {
                            "kernelInfo" => $kernelInfo
                        },
                        "command" => $envelope,
                        "routingSlip" => [
                            $kernelUri
                        ]
                    });
                    $succeeded = true;
                } elsif ($commandType eq "RequestValue") {
                    #
                    #                                              RequestValue
                    #
                    $valueName = $command->{'name'};
                    $mimeType = $command->{'mimeType'};
                    $formattedValue = getStringRepresentationOfValueName($valueName, $mimeType);
                    publish({
                        "eventType" => "ValueProduced",
                        "event" => {
                            "name" => $valueName,
                            "formattedValue" => {
                                "mimeType" => $mimeType,
                                "value" => $formattedValue
                            }
                        },
                        "command" => $envelope,
                        "routingSlip" => [
                            $kernelUri
                        ]
                    });
                    $succeeded = true;
                } elsif ($commandType eq "RequestValueInfos") {
                    #
                    #                                         RequestValueInfos
                    #
                    $mimeType = $command->{'mimeType'};
                    my @valueInfos = ();
                    foreach my $valueName ( keys %main:: ) {
                        if (!$suppressedValues{$valueName}) {
                            $formattedValue = getStringRepresentationOfValueName($valueName, $mimeType);
                            $typeName = getTypeNameOfValueName($valueName);
                            push(@valueInfos, {
                                "typeName" => $typeName,
                                "name" => "$valueName",
                                "formattedValue" => {
                                    "mimeType" => $mimeType,
                                    "value" => $formattedValue
                                },
                                "preferredMimeTypes" => [
                                    "text/plain",
                                    "application/json"
                                ]
                            });
                        }
                    }
                    publish({
                        "eventType" => "ValueInfosProduced",
                        "event" => {
                            "valueInfos" => \@valueInfos
                        },
                        "command" => $envelope,
                        "routingSlip" => [
                            $kernelUri
                        ]
                    });
                    $succeeded = true;
                } elsif ($commandType eq "SendValue") {
                    #
                    #                                                 SendValue
                    #
                    $formattedValue = $command->{'formattedValue'};
                    # if `application/json` or `text/json`
                    if ($formattedValue->{'mimeType'} =~ m/\/json$/) {
                        $valueName = $command->{'name'};
                        $jsonValue = $formattedValue->{'value'};
                        $runtimeValue = decode_json($jsonValue);
                        $main::{$valueName} = $runtimeValue;
                        $succeeded = true;
                    }
                } elsif ($commandType eq "SubmitCode") {
                    #
                    #                                                SubmitCode
                    #
                    $code = $command->{'code'};
                    ($stdout, $stderr, $result) = capture {
                        return eval $code;
                    };
                    #$result = eval $code;
                    if ($stdout ne "") {
                        publish({
                            "eventType" => "StandardOutputValueProduced",
                            "event" => {
                                "formattedValues" => [{
                                    "mimeType" => "text/plain",
                                    "value" => $stdout
                                }]
                            },
                            "command" => $envelope,
                            "routingSlip" => [
                                $kernelUri
                            ]
                        });
                    }
                    if ($stderr ne "") {
                        publish({
                            "eventType" => "StandardErrorValueProduced",
                            "event" => {
                                "formattedValues" => [{
                                    "mimeType" => "text/plain",
                                    "value" => $stderr
                                }]
                            },
                            "command" => $envelope,
                            "routingSlip" => [
                                $kernelUri
                            ]
                        });
                    }
                    publish({
                        "eventType" => "ReturnValueProduced",
                        "event" => {
                            "formattedValues" => [{
                                "mimeType" => "text/plain",
                                "value" => "$result"
                            }],
                        },
                        "command" => $envelope,
                        "routingSlip" => [
                            $kernelUri
                        ]
                    });
                    $succeeded = true;
                } else {
                    $succeeded = false;
                }

                push(@$envelopeRoutingSlip, $kernelUri);
                if ($succeeded) {
                    publish({
                        "eventType" => "CommandSucceeded",
                        "event" => {},
                        "command" => $envelope,
                        "routingSlip" => [
                            $kernelUri
                        ]
                    });
                } else {
                    publish({
                        "eventType" => "CommandFailed",
                        "event" => {
                            "message" => "Unknown command type: $commandType"
                        },
                        "command" => $envelope,
                        "routingSlip" => [
                            $kernelUri
                        ]
                    });
                }
            }
            $eventType = $envelope->{'eventType'};
            if ($eventType) {
                # TODO: respond to events
            }
        } catch {
            print STDERR "error: $_\n";
        }
    }
}

sub publish {
    print encode_json(\%{$_[0]}) . "\n";
}

sub getTypeNameOfValueName {
    my $valueName = shift;
    my $rawValue = $main::{$valueName};
    my @asArray = @{getArray($rawValue)};
    my %asHash = %{getHash($rawValue)};
    if (@asArray) {
        return "array";
    }
    elsif (%asHash) {
        return "HASH";
    }
    elsif ( length do { no warnings "numeric"; $$rawValue & '' }) {
        return "number";
    }
    else {
        return "scalar";
    }
}

sub getStringRepresentationOfValueName {
    my $valueName = shift;
    my $mimeType = shift;
    my $rawValue = $main::{$valueName};
    my $formattedValue;
    # if `application/json` or `text/json`
    if ($mimeType =~ m/\/json$/) {
        my @asArray = @{getArray($rawValue)};
        my %asHash = %{getHash($rawValue)};
        if (@asArray) {
            $rawValue = \@asArray;
        }
        elsif (%asHash) {
            $rawValue = \%asHash;
        }
        elsif ( length do { no warnings "numeric"; $$rawValue & '' }) {
            $rawValue = $$rawValue + 0;
        }
        else {
            $rawValue = $$rawValue;
        }

        $formattedValue = encode_json($rawValue);
    }
    else {
        # assume text/plain
        my @asArray = @{getArray($rawValue)};
        my %asHash = %{getHash($rawValue)};
        if (@asArray) {
            $formattedValue = "(" . join(", ", @asArray) . ")";
        }
        elsif (%asHash) {
            $formattedValue = "(" . join(", ", map { "$_ => $asHash{$_}" } keys %asHash) . ")";
        }
        else {
            $formattedValue = "" . $$rawValue;
        }
    }

    return $formattedValue;
}

sub getArray {
    my $rawValue = shift;
    if (ref($$rawValue) eq "ARRAY") {
        return \@$$rawValue;
    }
    elsif (@$rawValue) {
        return \@$rawValue;
    }

    return undef;
}

sub getHash {
    my $rawValue = shift;
    if (ref($$rawValue) eq "HASH") {
        return \%$$rawValue;
    }
    elsif (%$rawValue) {
        return \%$rawValue;
    }

    return undef;
}
