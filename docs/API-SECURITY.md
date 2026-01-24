# API Security Implementation

This document describes the API security implementation that ensures only genuine builds of the app can call the APIs.

## Overview

The API security system uses:
1. **API Key Authentication** - A shared secret key between the app and APIs
2. **Request Signing** - HMAC-SHA256 signatures to verify request authenticity
3. **Middleware-based Enforcement** - Automatic validation on all API endpoints

## Architecture

### Server-Side (APIs)

Both `FWH.MarketingApi` and `FWH.Location.Api` include:
- `ApiKeyAuthenticationMiddleware` - Validates API keys and request signatures
- Configuration via `appsettings.json` under `ApiSecurity` section

### Client-Side (Mobile App)

The mobile app includes:
- `ApiAuthenticationService` - Generates API keys and request signatures
- `ApiAuthenticationMessageHandler` - Automatically adds authentication headers to HTTP requests
- Configuration via `appsettings.json` under `ApiSecurity` section

## Configuration

### API Configuration (appsettings.json)

```json
{
  "ApiSecurity": {
    "ApiKey": "your-api-key-here",
    "ApiSecret": "your-api-secret-here",
    "RequireAuthentication": true
  }
}
```

**Important Security Notes:**
- In production, use strong, randomly generated keys
- Store secrets securely (use environment variables or secret management)
- Never commit production keys to source control
- Use different keys for different environments (dev, staging, production)

### Mobile App Configuration (appsettings.json)

```json
{
  "ApiSecurity": {
    "ApiKey": "your-api-key-here",
    "ApiSecret": "your-api-secret-here"
  }
}
```

The mobile app uses the same API key and secret as the APIs to authenticate requests.

## How It Works

### Request Flow

1. **Mobile App** creates an HTTP request
2. **ApiAuthenticationMessageHandler** intercepts the request
3. **ApiAuthenticationService** adds:
   - `X-API-Key` header with the API key
   - `X-Request-Signature` header with HMAC-SHA256 signature
4. Request is sent to the API
5. **ApiKeyAuthenticationMiddleware** validates:
   - API key matches configured key
   - Request signature is valid (if present)
6. Request proceeds if valid, or returns 401 Unauthorized if invalid

### Request Signing

The request signature is computed as:
```
stringToSign = method + path + query + bodyHash
signature = HMAC-SHA256(stringToSign, apiSecret)
```

Where:
- `method` = HTTP method (GET, POST, etc.)
- `path` = Request path (e.g., `/api/marketing/nearby`)
- `query` = Query string (e.g., `?latitude=40.7128&longitude=-74.0060`)
- `bodyHash` = Base64-encoded SHA256 hash of request body (if present)

## Security Considerations

### Current Implementation

✅ **Implemented:**
- API key validation
- Request signing with HMAC-SHA256
- Automatic header injection via message handler
- Configurable authentication (can be disabled in development)

⚠️ **Limitations:**
- API keys are embedded in the app (can be extracted by reverse engineering)
- Request signing is optional (middleware checks if signature is present)
- No certificate pinning
- No platform-specific app attestation (iOS App Attestation, Android Play Integrity)

### Recommended Enhancements

For production, consider adding:

1. **Platform-Specific App Attestation**
   - iOS: Use [App Attestation](https://developer.apple.com/documentation/devicecheck/validating_apps_that_connect_to_your_server)
   - Android: Use [Play Integrity API](https://developer.android.com/google/play/integrity)

2. **Certificate Pinning**
   - Pin API server certificates to prevent MITM attacks
   - Use platform-specific certificate pinning libraries

3. **Obfuscation**
   - Obfuscate API keys in the app binary
   - Use code obfuscation tools to make reverse engineering harder

4. **Dynamic Key Exchange**
   - Implement a key exchange protocol
   - Rotate keys periodically
   - Use time-based tokens

5. **Rate Limiting**
   - Add rate limiting per API key
   - Monitor for suspicious activity
   - Block compromised keys

## Development vs Production

### Development

In development, authentication can be disabled:
```json
{
  "ApiSecurity": {
    "RequireAuthentication": false
  }
}
```

This allows:
- Testing without authentication
- Swagger UI access without authentication
- Easier debugging

### Production

In production:
- Set `RequireAuthentication: true`
- Use strong, randomly generated keys
- Store secrets in secure configuration (environment variables, Azure Key Vault, etc.)
- Monitor authentication failures
- Implement key rotation strategy

## Testing

### Testing Authentication

1. **Valid Request:**
   ```bash
   curl -H "X-API-Key: your-api-key" \
        -H "X-Request-Signature: computed-signature" \
        https://api.example.com/api/marketing/nearby
   ```

2. **Invalid API Key:**
   ```bash
   curl -H "X-API-Key: wrong-key" \
        https://api.example.com/api/marketing/nearby
   # Returns: 401 Unauthorized
   ```

3. **Missing API Key:**
   ```bash
   curl https://api.example.com/api/marketing/nearby
   # Returns: 401 Unauthorized
   ```

## Troubleshooting

### Common Issues

1. **401 Unauthorized Errors**
   - Check that API keys match between app and API
   - Verify `RequireAuthentication` is set correctly
   - Check that authentication middleware is registered

2. **Signature Validation Fails**
   - Ensure request body is included in signature calculation
   - Verify HMAC-SHA256 implementation matches on both sides
   - Check that query string is included in signature

3. **Authentication Not Applied**
   - Verify `ApiAuthenticationMessageHandler` is registered
   - Check that HTTP clients are created via factory
   - Ensure middleware is added to the pipeline

## Files Modified

### Server-Side
- `src/FWH.MarketingApi/Middleware/ApiKeyAuthenticationMiddleware.cs` (new)
- `src/FWH.MarketingApi/Program.cs` (updated)
- `src/FWH.Location.Api/Middleware/ApiKeyAuthenticationMiddleware.cs` (new)
- `src/FWH.Location.Api/Program.cs` (updated)
- `src/FWH.MarketingApi/appsettings.json` (updated)
- `src/FWH.Location.Api/appsettings.json` (updated)

### Client-Side
- `src/FWH.Mobile/FWH.Mobile/Services/ApiAuthenticationService.cs` (new)
- `src/FWH.Mobile/FWH.Mobile/App.axaml.cs` (updated)
- `src/FWH.Mobile/FWH.Mobile/appsettings.json` (updated)
- `src/FWH.Orchestrix.Mediator.Remote/Extensions/MediatorServiceCollectionExtensions.cs` (updated)

## Future Work

- [ ] Add platform-specific app attestation (iOS/Android)
- [ ] Implement certificate pinning
- [ ] Add key rotation mechanism
- [ ] Implement rate limiting per API key
- [ ] Add monitoring and alerting for authentication failures
- [ ] Create key management service for production
