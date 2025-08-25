#!/bin/bash

# TiDB.Vector.NET Publishing Script
# Usage: ./publish.sh <version> [target] [nuget-api-key]

set -e  # Exit on any error

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Function to print colored output
print_status() {
    echo -e "${BLUE}📝 $1${NC}"
}

print_success() {
    echo -e "${GREEN}✅ $1${NC}"
}

print_warning() {
    echo -e "${YELLOW}⚠️  $1${NC}"
}

print_error() {
    echo -e "${RED}❌ $1${NC}"
}

# Check if we have the required arguments
if [ $# -lt 1 ]; then
    echo "Usage: $0 <version> [target] [nuget-api-key]"
    echo "Example: $0 1.1.0 release"
    echo "Example: $0 1.1.0 debug your-api-key"
    exit 1
fi

VERSION=$1
TARGET=${2:-Release}  # Default to Release if not specified
NUGET_API_KEY=${3:-$NUGET_API_KEY}  # Use environment variable if not provided

# Validate version format (simple check)
if [[ ! $VERSION =~ ^[0-9]+\.[0-9]+(\.[0-9]+)?$ ]]; then
    print_error "Invalid version format. Use format like: 1.1.0, 1.1, etc."
    exit 1
fi

# Check if we have a NuGet API key
if [ -z "$NUGET_API_KEY" ]; then
    print_error "NuGet API key is required. Either pass it as third argument or set NUGET_API_KEY environment variable."
    exit 1
fi

print_success "🚀 Publishing TiDB.Vector.NET v$VERSION ($TARGET)"

# Check if we're in the right directory
if [ ! -f "TiDB.Vector.NET.sln" ]; then
    print_error "Please run this script from the TiDB.Vector.NET root directory"
    exit 1
fi

# Backup current .csproj files
print_status "Creating backup of .csproj files..."
mkdir -p .backup
cp TiDB.Vector/TiDB.Vector.csproj .backup/
cp TiDB.Vector.AzureOpenAI/TiDB.Vector.AzureOpenAI.csproj .backup/

# Update versions in all .csproj files
print_status "Updating versions to $VERSION..."
find . -name "*.csproj" -exec sed -i '' "s/<Version>.*<\/Version>/<Version>$VERSION<\/Version>/g" {} \;

# Verify version updates
print_status "Verifying version updates..."
for csproj in TiDB.Vector/TiDB.Vector.csproj TiDB.Vector.AzureOpenAI/TiDB.Vector.AzureOpenAI.csproj; do
    if grep -q "<Version>$VERSION</Version>" "$csproj"; then
        print_success "Updated $csproj to version $VERSION"
    else
        print_error "Failed to update version in $csproj"
        exit 1
    fi
done

# Clean previous builds
print_status "Cleaning previous builds..."
dotnet clean

# Build all projects
print_status "Building packages in $TARGET configuration..."
dotnet build -c $TARGET

# Check if build was successful
if [ $? -ne 0 ]; then
    print_error "Build failed! Aborting publish."
    exit 1
fi

print_success "Build completed successfully!"

# Publish packages in dependency order
print_status "Publishing TiDB.Vector (core package)..."
dotnet pack TiDB.Vector/TiDB.Vector.csproj -c $TARGET
if [ $? -eq 0 ]; then
    print_success "TiDB.Vector packed successfully"
else
    print_error "Failed to pack TiDB.Vector"
    exit 1
fi

print_status "Publishing TiDB.Vector.AzureOpenAI..."
dotnet pack TiDB.Vector.AzureOpenAI/TiDB.Vector.AzureOpenAI.csproj -c $TARGET
if [ $? -eq 0 ]; then
    print_success "TiDB.Vector.AzureOpenAI packed successfully"
else
    print_error "Failed to pack TiDB.Vector.AzureOpenAI"
    exit 1
fi

# Push to NuGet
print_status "Pushing TiDB.Vector to NuGet..."
dotnet nuget push TiDB.Vector/bin/$TARGET/TiDB.Vector.$VERSION.nupkg --api-key $NUGET_API_KEY --source https://api.nuget.org/v3/index.json
if [ $? -eq 0 ]; then
    print_success "TiDB.Vector v$VERSION published to NuGet!"
else
    print_error "Failed to publish TiDB.Vector to NuGet"
    exit 1
fi

print_status "Pushing TiDB.Vector.AzureOpenAI to NuGet..."
dotnet nuget push TiDB.Vector.AzureOpenAI/bin/$TARGET/TiDB.Vector.AzureOpenAI.$VERSION.nupkg --api-key $NUGET_API_KEY --source https://api.nuget.org/v3/index.json
if [ $? -eq 0 ]; then
    print_success "TiDB.Vector.AzureOpenAI v$VERSION published to NuGet!"
else
    print_error "Failed to publish TiDB.Vector.AzureOpenAI to NuGet"
    exit 1
fi

# Create git tag
print_status "Creating git tag v$VERSION..."
git add .
git commit -m "chore: bump version to $VERSION" || print_warning "No changes to commit"
git tag -a "v$VERSION" -m "Release version $VERSION"

print_success "🎉 All packages published successfully!"
print_success "Version: $VERSION"
print_success "Configuration: $TARGET"
print_success "Git tag created: v$VERSION"

print_status "Next steps:"
echo "  1. Push the tag: git push origin v$VERSION"
echo "  2. Push changes: git push origin development"
echo "  3. Verify packages on NuGet.org"

# Optional: Ask if user wants to push to git
read -p "Do you want to push the tag and changes to git now? (y/N): " -n 1 -r
echo
if [[ $REPLY =~ ^[Yy]$ ]]; then
    print_status "Pushing to git..."
    git push origin v$VERSION
    git push origin development
    print_success "Git push completed!"
fi
