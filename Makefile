OUTPUT_DIR = ./_Output/
PROGRAM_NAME = gui

publish:

	rm -rf ${OUTPUT_DIR}
	
	# app
	dotnet publish AvaloniaUI/AvaloniaUI.csproj \
		-c Release \
		-r linux-x64 \
		--self-contained true \
		/p:PublishSingleFile=false \
		/p:IncludeAllContentForSelfExtract=true \
		-o ${OUTPUT_DIR}/${PROGRAM_NAME}
		
	cd Engine && mkdir -p build && cd build && cmake -DCMAKE_BUILD_TYPE='Release' .. && make
	cp -r Engine/build/output ${OUTPUT_DIR}/${PROGRAM_NAME}/Engine
		
	#tar -czvf ${OUTPUT_DIR}/GameLibrary.Avalonia.tar.gz -C ${OUTPUT_DIR} Avalonia
	
engine:
	cd Engine && mkdir -p build && cd build && cmake -DCMAKE_BUILD_TYPE='Release' .. && make