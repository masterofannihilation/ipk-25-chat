PROJECT_NAME = ipk-25-chat
EXECUTABLE = ipk25chat-client
OUTPUT_DIR = .

# Default target
all: build

# Build a self-contained single-file executable for Linux
build:
	dotnet publish $(PROJECT_NAME).csproj -c Release -r linux-x64 \
		--self-contained true \
		-p:PublishSingleFile=true \
		-p:AssemblyName=$(EXECUTABLE) \
		-o $(OUTPUT_DIR)

# Clean build artifacts
clean:
	dotnet clean
	rm -rf $(PROJECT_NAME) bin obj *.pdb *.sln

.PHONY: all build clean