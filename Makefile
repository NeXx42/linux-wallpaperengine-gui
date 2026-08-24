OUTPUT_DIR = ./Build/Output/
PROGRAM_NAME = TuxPaperEngine

build:
	rm -rf ${OUTPUT_DIR}/*
	
	# build bridge
	cd SteamCMDBridge && \
		python3 -m venv .env && \
		./.env/bin/pip install -r requirements.txt && \
		./.env/bin/pyinstaller steamcmd_bridge.py --onefile
	
	# build program
	mkdir -p ${OUTPUT_DIR}/${PROGRAM_NAME}
	dotnet publish AvaloniaUI/AvaloniaUI.csproj \
		-c Release \
		-r linux-x64 \
		--self-contained true \
		/p:PublishSingleFile=false \
		-o ${OUTPUT_DIR}/${PROGRAM_NAME}	

publish-appimage:
	rm -rf ${OUTPUT_DIR}/*
	
	# build bridge
	cd SteamCMDBridge && \
		python3 -m venv .env && \
		./.env/bin/pip install -r requirements.txt && \
		./.env/bin/pyinstaller steamcmd_bridge.py --onefile
	
	# build program
	mkdir -p ${OUTPUT_DIR}/${PROGRAM_NAME}
	dotnet publish AvaloniaUI/AvaloniaUI.csproj \
		-c Release \
		-r linux-x64 \
		--self-contained true \
		/p:PublishSingleFile=false \
		-o ${OUTPUT_DIR}/${PROGRAM_NAME}
		
	mkdir -p ${OUTPUT_DIR}/${PROGRAM_NAME}.AppDir/usr/bin
	
	cp -r ${OUTPUT_DIR}/${PROGRAM_NAME}/* ${OUTPUT_DIR}/${PROGRAM_NAME}.AppDir/usr/bin
	cp ./Build/AppImageData/* ${OUTPUT_DIR}/${PROGRAM_NAME}.AppDir
	
	ARCH=x86_64 appimagetool ${OUTPUT_DIR}/${PROGRAM_NAME}.AppDir ${OUTPUT_DIR}/${PROGRAM_NAME}.appimage
	chmod +x ${OUTPUT_DIR}/${PROGRAM_NAME}.appimage
	
publish-flatpak:
	rm -rf ${OUTPUT_DIR}/*
	
	# build bridge
	cd SteamCMDBridge && \
		python3 -m venv .env && \
		./.env/bin/pip install -r requirements.txt && \
		./.env/bin/pyinstaller steamcmd_bridge.py --onefile
	
	# build program
	mkdir -p ${OUTPUT_DIR}/${PROGRAM_NAME}
	dotnet publish AvaloniaUI/AvaloniaUI.csproj \
		-c Release \
		-r linux-x64 \
		--self-contained true \
		/p:PublishSingleFile=false \
		-o ${OUTPUT_DIR}/${PROGRAM_NAME}

	# -----------------------------[ flatpak ]-----------------------------
	mkdir -p ${OUTPUT_DIR}/${PROGRAM_NAME}
	
	flatpak-builder \
		--force-clean \
		--user \
		--install \
		flatpak-build \
		./Build/Flatpak/com.nexx.TuxPaperEngine.yml	
