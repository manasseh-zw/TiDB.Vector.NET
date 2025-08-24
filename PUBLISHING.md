# Publishing TiDB.Vector.NET to NuGet

This guide walks you through publishing the three NuGet packages to nuget.org.

## 📦 Package Structure

1. **`TiDB.Vector`** - Core package with vector store functionality + OpenAI integration built-in
   - Target Framework: .NET 8.0
   - Dependencies: drittich.SemanticSlicer, MySqlConnector, OpenAI SDK
   
2. **`TiDB.Vector.AzureOpenAI`** - Azure OpenAI integration (extends TiDB.Vector)
   - Target Framework: .NET 8.0
   - Dependencies: Azure.AI.OpenAI, Azure.Identity + TiDB.Vector
   

   - Target Framework: .NET 8.0
   - Dependencies: OpenAI SDK + TiDB.Vector

## 🎯 Package Alignment

All packages are configured with consistent settings:
- **Target Framework**: .NET 8.0 (all packages)
- **Version**: 1.0.0 (all packages)
- **Authors**: manasseh-zw
- **License**: MIT
- **Repository**: https://github.com/manasseh-zw/TiDB.Vector.NET

## 🚀 Pre-Publishing Checklist

### 1. Update Version Numbers
Update the version in each project file:
```xml
<Version>1.0.0</Version>
```

### 2. Create LICENSE File
Ensure you have a LICENSE file in the root directory (MIT recommended).

### 3. Test Build
```bash
dotnet build --configuration Release
```

### 4. Test Pack
```bash
dotnet pack --configuration Release --no-build
```

## 📋 Publishing Steps

### Step 1: Get NuGet API Key
1. Go to [nuget.org](https://www.nuget.org)
2. Sign in with your account
3. Go to Account Settings → API Keys
4. Create a new API key with "Push" permissions
5. Copy the API key

### Step 2: Publish Core Package
```bash
cd TiDB.Vector
dotnet pack --configuration Release
dotnet nuget push bin/Release/TiDB.Vector.1.0.0.nupkg --api-key YOUR_API_KEY --source https://api.nuget.org/v3/index.json
```

### Step 3: Publish OpenAI Package
```bash

```

### Step 4: Publish Azure OpenAI Package
```bash
cd ../TiDB.Vector.AzureOpenAI
dotnet pack --configuration Release
dotnet nuget push bin/Release/TiDB.Vector.AzureOpenAI.1.0.0.nupkg --api-key YOUR_API_KEY --source https://api.nuget.org/v3/index.json
```

## 🔧 Alternative: Using .nuspec Files

If you prefer more control, you can use the .nuspec files:

```bash
# Core package
nuget pack TiDB.Vector/TiDB.Vector.nuspec -OutputDirectory ./packages
nuget push ./packages/TiDB.Vector.1.0.0.nupkg -ApiKey YOUR_API_KEY -Source https://api.nuget.org/v3/index.json

# OpenAI package


# Azure OpenAI package
nuget pack TiDB.Vector.AzureOpenAI/TiDB.Vector.AzureOpenAI.nuspec -OutputDirectory ./packages
nuget push ./packages/TiDB.Vector.AzureOpenAI.1.0.0.nupkg -ApiKey YOUR_API_KEY -Source https://api.nuget.org/v3/index.json
```

## 📝 Package Dependencies

- **TiDB.Vector**: No dependencies on other TiDB.Vector packages

- **TiDB.Vector.AzureOpenAI**: Depends on TiDB.Vector

## 🎯 Publishing Order

**Important**: Publish packages in this order:
1. `TiDB.Vector` (core package with OpenAI built-in)
2. `TiDB.Vector.AzureOpenAI` (depends on core)



## 🔍 Verification

After publishing:
1. Check [nuget.org](https://www.nuget.org) for your packages
2. Test installation in a new project:
   ```bash
   dotnet new console
   dotnet add package TiDB.Vector
   
   ```

## 🚨 Common Issues

### Package Already Exists
- Update version number before republishing
- NuGet doesn't allow overwriting existing versions

### Dependency Issues
- Ensure core package is published before extension packages
- Check that dependency versions are correct

### Build Errors
- Ensure all projects build in Release mode
- Check that all required files are included in the package

## 📚 Additional Resources

- [NuGet Package Publishing Guide](https://docs.microsoft.com/en-us/nuget/quickstart/create-and-publish-a-package)
- [NuGet Package Versioning](https://docs.microsoft.com/en-us/nuget/concepts/package-versioning)
- [NuGet Package Dependencies](https://docs.microsoft.com/en-us/nuget/concepts/package-dependencies)
