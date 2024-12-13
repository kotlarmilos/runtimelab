#!/bin/bash

if [ "$#" -lt 4 ]; then
    echo "Usage: $0 <platform> <architecture> <framework1> [<framework2> ...]"
    exit 1
fi

# Parse arguments
PLATFORM=$1
ARCHITECTURE=$2

shift 2
# Remove framework bindings if --metadata-only flag is passed
REMOVE_BINDINGS=false
FRAMEWORKS=()

for arg in "$@"; do
    if [ "$arg" == "--metadata-only" ]; then
        REMOVE_BINDINGS=true
    else
        FRAMEWORKS+=("$arg")
    fi
done

# Output directory for generated bindings
OUTPUT_DIR="./GeneratedBindings"
rm -rf "$OUTPUT_DIR"
mkdir -p "$OUTPUT_DIR"

cd $OUTPUT_DIR

# Function to extract ABI file
extract_abi_json() {
    local framework=$1

    local sdk_path=$(xcrun -sdk $(echo "$PLATFORM" | tr '[:upper:]' '[:lower:]') --show-sdk-path)
    local swift_interface_path="/Applications/Xcode.app/Contents/Developer/Platforms/${PLATFORM}.platform/Developer/SDKs/${PLATFORM}.sdk/System/Library/Frameworks/${framework}.framework/Versions/Current/Modules/${framework}.swiftmodule/${ARCHITECTURE}.swiftinterface"

    if [ ! -f "$swift_interface_path" ]; then
        echo "Error: Swift interface file not found for framework '$framework'."
        return 1
    fi

    xcrun swift-frontend -compile-module-from-interface "$swift_interface_path" \
        -module-name "$framework" \
        -sdk "$sdk_path" \
        -emit-abi-descriptor-path "./${framework}.abi.json"
}

# Function to generate bindings
generate_dotnet_bindings() {
    local framework=$1

    dotnet ../artifacts/bin/Swift.Bindings/Release/net9.0/Swift.Bindings.dll -a "$framework" -o "$OUTPUT_DIR"

    if $REMOVE_BINDINGS; then
        rm -rf "./Swift.$framework.cs"
    fi
}

# Function to generate NuGet package
generate_nuget_package() {
    local project_file="./Swift.Bindings.${PLATFORM}.Experimental.csproj"

    cat <<EOL > "$project_file"
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <AllowUnsafeBlocks>true</AllowUnsafeBlocks>
    <IsPackable>true</IsPackable>
  </PropertyGroup>
</Project>
EOL

    dotnet pack "$project_file"
}

# Process each framework
for framework in "${FRAMEWORKS[@]}"; do
    echo "Processing framework: $framework"

    if extract_abi_json "$framework"; then
        generate_dotnet_bindings "$framework"
    else
        echo "Skipping framework '$framework' due to errors."
    fi
done

generate_nuget_package

echo "Process completed."
