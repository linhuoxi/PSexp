#target photoshop
// 切片/图层批量导出（遍历可见一级图层，分别导出为 JPG/PNG）
// 跟 layers.jsx 一样使用 doc.saveAs，避免 ExportOptionsSaveForWeb 在新版 PS 上的静默失败
// 占位符: {{EXPORT_PATH}} {{FORMAT}} {{QUALITY}} {{FILE_NAME}} {{RENAME}} {{IMAGE_NAME}}
function exportSlices() {
    function jsonStr(s) {
        s = String(s).replace(/\\/g, '\\\\').replace(/"/g, '\\"');
        return '"' + s + '"';
    }
    function safeName(s) {
        return String(s).replace(/[\/:*?"<>|]/g, '_');
    }
    var exportPath = '{{EXPORT_PATH}}';
    var format = '{{FORMAT}}';
    var quality = {{QUALITY}};
    var fileBase = '{{FILE_NAME}}';
    var rename = {{RENAME}};
    var imageName = '{{IMAGE_NAME}}';
    // IMAGE_NAME 不为空时作为前缀
    fileBase = safeName(fileBase);
    if (imageName) fileBase = safeName(imageName) + '_' + fileBase;
    try {
        if (app.documents.length === 0) {
            $.writeln('PSX_LOG|error|no open document');
            return '{"success":false,"count":0,"error":"no document"}';
        }
        var doc = app.activeDocument;
        var layers = doc.layers;
        var total = layers.length;

        // 记录可见性
        var visMap = {};
        for (var v = 0; v < total; v++) {
            visMap[layers[v].id] = layers[v].visible;
        }

        $.writeln('PSX_LOG|info|start export slices, total=' + total + ' base=' + fileBase);
        var count = 0;
        var ext = format.toLowerCase();
        for (var i = 0; i < total; i++) {
            var layer = layers[i];
            if (!layer.visible || /^排除/i.test(layer.name)) continue;

            // 只显示当前图层
            for (var h = 0; h < total; h++) {
                layers[h].visible = (layers[h] === layer);
            }

            var sliceName = fileBase + '_' + count;
            var outFile = new File(exportPath + sliceName + '.' + ext);
            if (rename) {
                var idx = 1;
                while (outFile.exists) {
                    outFile = new File(exportPath + fileBase + '_' + count + '_' + idx + '.' + ext);
                    idx++;
                }
            } else if (outFile.exists && !outFile.remove()) {
                throw new Error('无法覆盖同名文件: ' + outFile.fsName);
            }

            var options;
            if (ext === 'png') {
                options = new PNGSaveOptions();
            } else {
                options = new JPEGSaveOptions();
                options.quality = Math.round(12 * (quality / 100));
            }
            doc.saveAs(outFile, options, true);
            count++;
            $.writeln('PSX_LOG|info|exported slice ' + count + ': ' + layer.name + ' -> ' + sliceName + '.' + ext);
        }

        // 还原可见性
        for (var r = 0; r < total; r++) {
            if (visMap[layers[r].id] !== undefined) layers[r].visible = visMap[layers[r].id];
        }
        $.writeln('PSX_LOG|info|slices done: ' + count);
        return '{"success":true,"count":' + count + '}';
    } catch (e) {
        // 还原可见性（异常路径）
        try {
            for (var r2 = 0; r2 < total; r2++) {
                if (visMap[layers[r2].id] !== undefined) layers[r2].visible = visMap[layers[r2].id];
            }
        } catch (e2) {}
        $.writeln('PSX_LOG|error|exportSlices: ' + e);
        return '{"success":false,"count":0,"error":' + jsonStr(String(e)) + '}';
    }
}
__psx_result = exportSlices();
