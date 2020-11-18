import {
    MessageTransport,
    KernelTransport,
    KernelEventEnvelopeObserver,
    KernelCommand,
    KernelCommandType,
    KernelCommandEnvelope,
    DisposableSubscription,
    KernelCommandEnvelopeObserver,
    KernelEventEnvelope,
    KernelEvent,
    KernelEventType
} from "./contracts";

export function kernelTransportFromMessageTransport(messageTransport: MessageTransport): KernelTransport {

    return {
        subscribeToKernelEvents: (observer: KernelEventEnvelopeObserver): DisposableSubscription => {
            return messageTransport.subscribeToMessagesWithLabel(
                "kernelEvents",
                observer);
        },

        submitCommand: (command: KernelCommand, commandType: KernelCommandType, token: string): Promise<void> => {
            let envelope: KernelCommandEnvelope = {
                commandType: commandType,
                command: command,
                token: token,
            };
            return messageTransport.sendMessage("kernelCommands", envelope);
        },

        subscribeToCommands: (observer: KernelCommandEnvelopeObserver): DisposableSubscription => {
            return messageTransport.subscribeToMessagesWithLabel(
                "kernelCommands",
                observer);
        },

        submitKernelEvent: (event: KernelEvent, eventType: KernelEventType, associatedCommand?: { command: KernelCommand, commandType: KernelCommandType, token: string }): Promise<void> => {
            let envelope: KernelEventEnvelope = {
                eventType: eventType,
                event: event,
                command: associatedCommand,
            };
            return messageTransport.sendMessage("kernelEvents", envelope);
        },

        waitForReady: (): Promise<void> => {
            return messageTransport.waitForReady();
        },

        dispose: (): void => {
        }
    };
}