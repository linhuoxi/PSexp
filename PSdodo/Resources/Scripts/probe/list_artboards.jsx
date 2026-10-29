#target photoshop
// 探针脚本：列出所有画板（JSON 数组字符串）
// 占位符：无
function listArtboards() {
    function jsonStr(s) {
        s = String(s);
        s = s.replace(/\\/g, '\\\\').replace(/"/g, '\\"')
             .replace(/\n/g, '\\n').replace(/\r/g, '\\r').replace(/\t/g, '\\t');
        return '"' + s + '"';
    }
    var result = [];
    try {
        if (app.documents.length === 0) { return '[]'; }
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
                var id = layerDesc.getInteger(stringIDToTypeID('layerID'));
                var name = '';
                try { name = layerDesc.getString(stringIDToTypeID('name')); } catch (e2) {}
                result.push({ id: id, name: name });
            }
        }
    } catch (e) {
        $.writeln('PSX_LOG|error|listArtboards: ' + e);
    }
    var parts = [];
    for (var j = 0; j < result.length; j++) {
        parts.push('{"id":' + result[j].id + ',"name":' + jsonStr(result[j].name) + '}');
    }
    $.writeln('PSX_LOG|info|found ' + result.length + ' artboards');
    return '[' + parts.join(',') + ']';
}
__psx_result = listArtboards();
