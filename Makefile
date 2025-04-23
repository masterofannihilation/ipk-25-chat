PROJECT_NAME = ipk-25-chat
EXECUTABLE = ipk25chat-client
OUTPUT_DIR = .

all: build

build:
	dotnet publish $(PROJECT_NAME).csproj -c Release -r linux-x64 \
		--self-contained true \
		-p:PublishSingleFile=true \
		-p:AssemblyName=$(EXECUTABLE) \
		-o $(OUTPUT_DIR)

clean:
	dotnet clean
	rm -rf $(EXECUTABLE) bin obj *.pdb *.sln

zip: clean
	zip -r xhatal02.zip $(OUTPUT_DIR) -x ".git/*" ".gitignore" ".idea/*" notes

.PHONY: all build clean zip