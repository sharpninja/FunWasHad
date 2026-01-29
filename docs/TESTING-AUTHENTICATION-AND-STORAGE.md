# Testing: Authentication and Blob Storage

This document describes the comprehensive test suite added for the authentication mechanism and blob storage functionality.

## Test Coverage

### Authentication Tests

#### 1. ApiKeyAuthenticationMiddlewareTests (Marketing API)
**Location:** `tests/FWH.MarketingApi.Tests/Middleware/ApiKeyAuthenticationMiddlewareTests.cs`

**Tests:**
- ✅ `InvokeAsync_ValidApiKey_AllowsRequest` - Validates requests with correct API key proceed
- ✅ `InvokeAsync_MissingApiKey_Returns401` - Rejects requests without API key
- ✅ `InvokeAsync_InvalidApiKey_Returns401` - Rejects requests with wrong API key
- ✅ `InvokeAsync_HealthCheckEndpoint_BypassesAuthentication` - Health checks skip auth
- ✅ `InvokeAsync_SwaggerEndpoint_BypassesAuthentication` - Swagger UI skips auth
- ✅ `InvokeAsync_ValidApiKeyAndSignature_AllowsRequest` - Valid signature validation
- ✅ `InvokeAsync_ValidApiKeyButInvalidSignature_Returns401` - Invalid signature rejection
- ✅ `Constructor_MissingApiKey_ThrowsInvalidOperationException` - Configuration validation
- ✅ `Constructor_MissingApiSecret_ThrowsInvalidOperationException` - Configuration validation

**Total:** 9 unit tests

#### 2. ApiKeyAuthenticationMiddlewareTests (Location API)
**Location:** `tests/FWH.Location.Api.Tests/Middleware/ApiKeyAuthenticationMiddlewareTests.cs`

**Tests:**
- ✅ `InvokeAsync_ValidApiKey_AllowsRequest`
- ✅ `InvokeAsync_MissingApiKey_Returns401`
- ✅ `InvokeAsync_InvalidApiKey_Returns401`

**Total:** 3 unit tests

#### 3. ApiAuthenticationServiceTests (Mobile App)
**Location:** `tests/FWH.Mobile.Tests/Services/ApiAuthenticationServiceTests.cs`

**Tests:**
- ✅ `AddAuthenticationHeaders_AddsApiKeyHeader` - Verifies API key header is added
- ✅ `AddAuthenticationHeaders_AddsRequestSignature` - Verifies signature header is added
- ✅ `AddAuthenticationHeaders_SignatureMatchesExpectedComputation` - Validates HMAC-SHA256 computation
- ✅ `AddAuthenticationHeaders_IncludesBodyHashInSignature` - Tests body hash in signature
- ✅ `Constructor_InvalidApiKey_ThrowsArgumentException` - Parameter validation (null, empty, whitespace)
- ✅ `Constructor_InvalidApiSecret_ThrowsArgumentException` - Parameter validation (null, empty, whitespace)

**Total:** 6 unit tests

#### 4. ApiKeyAuthenticationIntegrationTests
**Location:** `tests/FWH.MarketingApi.Tests/Integration/ApiKeyAuthenticationIntegrationTests.cs`

**Tests:**
- ✅ `AuthenticatedRequest_ReturnsSuccess` - End-to-end authenticated request
- ✅ `UnauthenticatedRequest_Returns401` - End-to-end unauthenticated rejection
- ✅ `HealthCheckEndpoint_BypassesAuthentication` - Health check exclusion

**Total:** 3 integration tests

### Blob Storage Tests

#### 5. LocalFileBlobStorageServiceTests
**Location:** `tests/FWH.MarketingApi.Tests/Services/LocalFileBlobStorageServiceTests.cs`

**Tests:**
- ✅ `UploadAsync_StoresFileAndReturnsUrl` - File upload and URL generation
- ✅ `UploadAsync_GeneratesUniqueFileNames` - Unique filename generation
- ✅ `UploadAsync_SanitizesFileName` - Security: directory traversal prevention
- ✅ `DeleteAsync_RemovesFile` - File deletion
- ✅ `DeleteAsync_NonExistentFile_ReturnsFalse` - Graceful handling of missing files
- ✅ `GetAsync_RetrievesFileStream` - File retrieval
- ✅ `GetAsync_NonExistentFile_ReturnsNull` - Graceful handling of missing files
- ✅ `ExistsAsync_ExistingFile_ReturnsTrue` - File existence check
- ✅ `ExistsAsync_NonExistentFile_ReturnsFalse` - File existence check
- ✅ `UploadAsync_NullStream_ThrowsArgumentNullException` - Parameter validation
- ✅ `UploadAsync_InvalidFileName_ThrowsArgumentException` - Parameter validation (null, empty, whitespace)
- ✅ `UploadWithThumbnailAsync_ReturnsStorageAndThumbnailUrls` - Thumbnail support

**Total:** 12 unit tests

#### 6. BlobStorageIntegrationTests
**Location:** `tests/FWH.MarketingApi.Tests/Services/BlobStorageIntegrationTests.cs`

**Tests:**
- ✅ `UploadImageAttachment_StoresFileAndCreatesRecord` - End-to-end image upload
- ✅ `UploadImageAttachment_FileIsRetrievable` - File retrieval after upload
- ✅ `UploadVideoAttachment_StoresFileAndCreatesRecord` - End-to-end video upload

**Total:** 3 integration tests

## Test Statistics

- **Total Test Files:** 6
- **Total Unit Tests:** 27
- **Total Integration Tests:** 6
- **Grand Total:** 33 tests

## Test Patterns

All tests follow the established patterns in the codebase:

1. **Comprehensive XMLDOC Comments** - Each test includes:
   - What is being tested
   - Data involved
   - Why the data matters
   - Expected outcome
   - Reason for expectation

2. **xUnit Framework** - Uses Fact and Theory attributes

3. **NSubstitute for Mocking** - Mocks ILogger and IConfiguration

4. **Test Fixtures** - Uses IClassFixture for shared test setup

5. **Proper Cleanup** - Implements IDisposable for file system tests

## Key Test Scenarios

### Authentication

1. **Valid Authentication Flow**
   - API key matches → Request proceeds
   - API key + valid signature → Request proceeds

2. **Invalid Authentication Flow**
   - Missing API key → 401 Unauthorized
   - Invalid API key → 401 Unauthorized
   - Invalid signature → 401 Unauthorized

3. **Bypass Scenarios**
   - Health check endpoints → No authentication required
   - Swagger endpoints → No authentication required

4. **Configuration Validation**
   - Missing API key → Exception at startup
   - Missing API secret → Exception at startup

### Blob Storage

1. **File Operations**
   - Upload → File stored, URL returned
   - Delete → File removed from disk
   - Get → File stream retrieved
   - Exists → File existence checked

2. **Security**
   - File name sanitization → Directory traversal prevented
   - Unique file names → No overwrites

3. **Error Handling**
   - Null parameters → ArgumentNullException
   - Invalid parameters → ArgumentException
   - Missing files → Graceful null/false returns

4. **Integration**
   - HTTP upload → File stored and database record created
   - File retrieval → Stored files accessible

## Running the Tests

### Run All Authentication and Storage Tests

```bash
# Marketing API tests (includes middleware and blob storage)
dotnet test tests/FWH.MarketingApi.Tests/FWH.MarketingApi.Tests.csproj --filter "FullyQualifiedName~ApiKey|FullyQualifiedName~BlobStorage"

# Location API tests (middleware)
dotnet test tests/FWH.Location.Api.Tests/FWH.Location.Api.Tests.csproj --filter "FullyQualifiedName~ApiKey"

# Mobile app tests (authentication service)
dotnet test tests/FWH.Mobile.Tests/FWH.Mobile.Tests.csproj --filter "FullyQualifiedName~ApiAuthentication"
```

### Run Specific Test Categories

```bash
# Unit tests only
dotnet test tests/FWH.MarketingApi.Tests/FWH.MarketingApi.Tests.csproj --filter "FullyQualifiedName~ApiKeyAuthenticationMiddlewareTests|FullyQualifiedName~LocalFileBlobStorageServiceTests"

# Integration tests only
dotnet test tests/FWH.MarketingApi.Tests/FWH.MarketingApi.Tests.csproj --filter "FullyQualifiedName~Integration"
```

## Test Files Created

### Authentication Tests
1. `tests/FWH.MarketingApi.Tests/Middleware/ApiKeyAuthenticationMiddlewareTests.cs`
2. `tests/FWH.Location.Api.Tests/Middleware/ApiKeyAuthenticationMiddlewareTests.cs`
3. `tests/FWH.Mobile.Tests/Services/ApiAuthenticationServiceTests.cs`
4. `tests/FWH.MarketingApi.Tests/Integration/ApiKeyAuthenticationIntegrationTests.cs`

### Blob Storage Tests
5. `tests/FWH.MarketingApi.Tests/Services/LocalFileBlobStorageServiceTests.cs`
6. `tests/FWH.MarketingApi.Tests/Services/BlobStorageIntegrationTests.cs`

## Test Infrastructure Updates

### CustomWebApplicationFactory
- Updated to configure blob storage for tests
- Creates temporary upload directory for each test run
- Configures API security settings for testing

## Coverage Areas

✅ **Authentication Middleware**
- API key validation
- Request signature validation
- Endpoint exclusions (health, swagger)
- Configuration validation
- Error responses

✅ **Authentication Service (Mobile)**
- Header injection
- Signature computation
- Parameter validation
- Body hash inclusion

✅ **Blob Storage Service**
- File upload
- File deletion
- File retrieval
- File existence checks
- File name sanitization
- Unique file name generation
- Error handling

✅ **Integration**
- HTTP request flow with authentication
- File upload through API
- File retrieval after upload
- Database record creation

## Notes

- All tests use temporary directories that are cleaned up after execution
- Integration tests use the actual HTTP pipeline with test containers
- Unit tests use mocks for isolation
- Tests follow the existing codebase patterns and conventions
