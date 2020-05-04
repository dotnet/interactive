#!/usr/bin/env bash

source="${BASH_SOURCE[0]}"

# resolve $source until the file is no longer a symlink
while [[ -h "$source" ]]; do
  scriptroot="$( cd -P "$( dirname "$source" )" && pwd )"
  source="$(readlink "$source")"
  # if $source was a relative symlink, we need to resolve it relative to the path where the
  # symlink file was located
  [[ $source != /* ]] && source="$scriptroot/$source"
done
scriptroot="$( cd -P "$( dirname "$source" )" && pwd )"

args=""
run_tests=false

# scan for `--test` or `-t`
while [[ $# > 0 ]]; do
  opt="$(echo "$1" | awk '{print tolower($0)}')"
  case "$opt" in
    --test|-t)
      run_tests=true
      ;;
  esac
  args="$args $1"
  shift
done

function TestUsingNPM() {
    test_path=$1
    pushd "$test_path"
    npm i
    npm run ciTest
    popd
}

# invoke regular build/test script
. "$scriptroot/common/build.sh" "/p:Projects=$scriptroot/../dotnet-interactive.sln" $args