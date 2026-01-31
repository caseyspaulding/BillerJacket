namespace BillerJacket.Contracts.Messaging;

public static class Queues
{
    public const string EmailSend = "email-send";
    public const string DunningEvaluate = "dunning-evaluate";
    public const string PaymentCommands = "payment-commands";
    public const string WebhookIngest = "webhook-ingest";
}
