#target photoshop
// 探针脚本：返回当前文档信息（JSON 字符串）
// 占位符：无
function probeDocumentInfo() {
    function jsonStr(s) {
        s = String(s);
        s = s.replace(/\\/g, '\\\\').replace(/"/g, '\\"')
             .replace(/\n/g, '\\n').replace(/\r/g, '\\r').replace(/\t/g, '\\t');
        return '"' + s + '"';
    }
    var empty = '{"name":"","hasArtboards":false,"artboardCount":0,"hasSlices":false,"width":0,"height":0,"path":""}';
    try {
        if (app.documents.length === 0) {
            $.writeln('PSX_LOG|info|no open document');
            return empty;
        }
        var doc = app.activeDocument;
        var name = doc.name;
        var width = 0, height = 0;
        try { width = Math.round(doc.width.as('px')); } catch (e) {}
        try { height = Math.round(doc.height.as('px')); } catch (e) {}
        var path = '';
        try { path = doc.path ? doc.path.fsName : ''; } catch (e) {}

        var abCount = 0;
        try {
            var docRef = new ActionReference();
            docRef.putEnumerated(charIDToTypeID('Dcmn'), charIDToTypeID('Ordn'), charIDToTypeID('Trgt'));
            var appDesc = executeActionGet(docRef);
            var numOfLayers = appDesc.getInteger(stringIDToTypeID('numberOfLayers'));
            for (var i = 1; i <= numOfLayers; i++) {
                var layerRef = new ActionReference();
                layerRef.putIndex(charIDToTypeID('Lyr '), i);
                var layerDesc = executeActionGet(layerRef);
                if (layerDesc.hasKey(stringIDToTypeID('artboardEnabled')) &&
                    layerDesc.getBoolean(stringIDToTypeID('artboardEnabled'))) {
                    abCount++;
                }
            }
        } catch (e) {
            $.writeln('PSX_LOG|warn|artboard count failed: ' + e);
        }

        // "切片导出"实际遍历一级图层，只要有文档就能用，无需切片检测
        var hasSlices = true;

        var json = '{"name":' + jsonStr(name) +
                   ',"hasArtboards":' + (abCount > 0 ? 'true' : 'false') +
                   ',"artboardCount":' + abCount +
                   ',"hasSlices":' + (hasSlices ? 'true' : 'false') +
                   ',"width":' + width +
                   ',"height":' + height +
                   ',"path":' + jsonStr(path) + '}';
        $.writeln('PSX_LOG|info|document=' + name + ' artboards=' + abCount + ' slices=' + hasSlices + ' size=' + width + 'x' + height);
        return json;
    } catch (e) {
        $.writeln('PSX_LOG|error|probeDocumentInfo: ' + e);
        return empty;
    }
}
__psx_result = probeDocumentInfo();
