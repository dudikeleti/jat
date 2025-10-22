using System.Diagnostics;
var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapGet("/ping", () => Sample.Work());

// Exception endpoints for testing Exception Replay with redacted variables
app.MapGet("/exception/argument", () => ExceptionThrowers.ThrowArgumentSensitive("secretToken123", "userPassword!", 42));
app.MapGet("/exception/nullref", () => ExceptionThrowers.ThrowNullReference("apiKey-XYZ", new SensitivePayload { creditCardNumber = "4111111111111111", ssn = "123-45-6789" }));
app.MapGet("/exception/invalidop", () => ExceptionThrowers.ThrowInvalidOperation("privateKeyPEM", new [] { "emailAddress", "phoneNumber" }));
app.MapGet("/exception/custom", () => ExceptionThrowers.ThrowCustom("sessionId-ABC", new Dictionary<string, string> { ["authToken"] = "abcdef", ["refreshToken"] = "uvwxyz" }));
app.MapGet("/exception/divzero", () => ExceptionThrowers.ThrowDivideByZero("encryptionKey-RSA2048", 100));

app.Run();

static class Sample
{
    public static string Work()
    {
        return "pong";
    }
}

static class ExceptionThrowers
{
    public static void ThrowArgumentSensitive(string apiKey, string password, int pinCode)
    {
        // Mix of redacted candidates and regular variables
        var userId = "user123";  // Regular - should be visible
        var requestCount = 5;    // Regular - should be visible
        var secretValue = $"{apiKey}:{password}:{pinCode}";  // Should be redacted
        var timestamp = DateTime.UtcNow;  // Regular - should be visible
        
        // create a distinct stack by delegating
        HelperLayerOne(secretValue, userId, requestCount);
    }

    private static void HelperLayerOne(string secretValue, string userId, int requestCount)
    {
        var operationName = "AuthenticateUser";  // Regular - should be visible
        var connectionString = "Server=prod;User Id=sa;Password=Adm1n!;";  // Should be redacted
        HelperLayerTwo(secretValue, connectionString, userId, operationName);
    }

    private static void HelperLayerTwo(string secretValue, string connectionString, string userId, string operationName)
    {
        var attemptNumber = 1;  // Regular - should be visible
        _ = attemptNumber;  // Used for snapshot testing
        throw new ArgumentException("Bad arguments provided for operation", nameof(secretValue));
    }

    public static void ThrowNullReference(string accessToken, SensitivePayload sensitivePayload)
    {
        object? obj = null;
        // Mix of redacted and regular variables
        var customerId = "CUST-789";  // Regular - should be visible
        var bearerToken = accessToken;  // Should be redacted
        var cardNumber = sensitivePayload.creditCardNumber;  // Should be redacted
        var socialSecurityNumber = sensitivePayload.ssn;  // Should be redacted
        var orderTotal = 199.99m;  // Regular - should be visible
        var itemCount = 3;  // Regular - should be visible
        
        // Used for snapshot testing
        _ = customerId; _ = orderTotal; _ = itemCount;
        
        // intentional NRE
        _ = obj!.ToString();
    }

    public static void ThrowInvalidOperation(string privateKey, string[] piiFields)
    {
        // Mix of redacted and regular variables
        var regionName = "us-east-1";  // Regular - should be visible
        var jwtSecret = privateKey;  // Should be redacted
        var fieldsToRedact = piiFields;  // Should be redacted
        var processingTime = 250;  // Regular - should be visible
        var batchSize = 100;  // Regular - should be visible
        _ = processingTime;  // Used for snapshot testing
        
        ProcessSensitiveData(jwtSecret, fieldsToRedact, regionName, batchSize);
    }

    private static void ProcessSensitiveData(string secret, string[] fields, string regionName, int batchSize)
    {
        var encryptionAlgorithm = "AES256";  // Regular - should be visible
        var certificatePath = "/certs/prod.pem";  // Regular - should be visible
        _ = encryptionAlgorithm; _ = certificatePath;  // Used for snapshot testing
        throw new InvalidOperationException("Operation not allowed in current state");
    }

    public static void ThrowCustom(string sessionId, Dictionary<string, string> credentials)
    {
        try
        {
            // Regular variables
            var requestId = Guid.NewGuid().ToString();  // Regular - should be visible
            var endpointName = "/api/authenticate";  // Regular - should be visible
            
            ThrowDeep(sessionId, credentials, requestId, endpointName);
        }
        catch (FormatException fe)
        {
            throw new ApplicationException("Wrapped custom exception", fe);
        }
    }

    private static void ThrowDeep(string sessionId, Dictionary<string, string> credentials, string requestId, string endpointName)
    {
        // Mix of redacted and regular
        var authToken = credentials.ContainsKey("authToken") ? credentials["authToken"] : "";  // Should be redacted
        var refreshToken = credentials.ContainsKey("refreshToken") ? credentials["refreshToken"] : "";  // Should be redacted
        var composite = $"{sessionId}:{authToken}:{refreshToken}";  // Should be redacted
        var attemptCount = 1;  // Regular - should be visible
        var maxRetries = 3;  // Regular - should be visible
        _ = maxRetries;  // Used for snapshot testing
        
        ThrowDeeperLayer(composite, authToken, requestId, attemptCount);
    }

    private static void ThrowDeeperLayer(string composite, string token, string requestId, int attemptCount)
    {
        var tokenHash = token.GetHashCode();  // Should be redacted (derived from token)
        var statusCode = 401;  // Regular - should be visible
        var responseTime = 125;  // Regular - should be visible
        _ = composite.Length; _ = statusCode; _ = responseTime;  // Used for snapshot testing
        throw new FormatException("Invalid composite token format");
    }

    public static void ThrowDivideByZero(string encryptionKey, int value)
    {
        // Mix of redacted and regular
        var moduleName = "PaymentProcessor";  // Regular - should be visible
        var privateKey = encryptionKey;  // Should be redacted
        var transactionId = "TXN-456";  // Regular - should be visible
        var amount = 500.00m;  // Regular - should be visible
        _ = amount;  // Used for snapshot testing
        
        var result = PerformCalculation(value, privateKey, moduleName, transactionId);
        _ = result;
    }

    private static int PerformCalculation(int numerator, string key, string moduleName, string transactionId)
    {
        var keyLength = key.Length;  // Should be redacted (derived from key)
        var operationType = "Decrypt";  // Regular - should be visible
        var cacheHit = false;  // Regular - should be visible
        _ = operationType; _ = cacheHit;  // Used for snapshot testing
        var denominator = keyLength - keyLength; // intentionally zero
        return numerator / denominator; // throws DivideByZeroException
    }
}

internal sealed class SensitivePayload
{
    public string? creditCardNumber { get; set; }
    public string? ssn { get; set; }
}
