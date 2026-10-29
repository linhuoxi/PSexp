#target photoshop
// 批量导出一级图层（每个文档根级图层单独导出一张图，含原有背景层）
// 占位符: {{EXPORT_PATH}} {{FORMAT}} {{QUALITY}} {{RENAME}} {{IMAGE_NAME}} {{WITH_DOC_NAME}} {{SHOW_HINT}}
function exportLayers() {
    function jsonStr(s) {
        s = String(s).replace(/\\/g, '\\\\').replace(/"/g, '\\"');
        return '"' + s + '"';
    }
    function safeName(s) {
        return String(s).replace(/[\/:*?"<>|]/g, '_');
    }
    function isBackgroundLayer(layer) {
        try {
            return layer.isBackgroundLayer === true;
        } catch (e) {
            return false;
        }
    }

    var exportPath = '{{EXPORT_PATH}}';
    var format = '{{FORMAT}}';
    var quality = {{QUALITY}};
    var rename = {{RENAME}};
    var imageName = '{{IMAGE_NAME}}';
    var withDocName = {{WITH_DOC_NAME}};
    var showHint = {{SHOW_HINT}};
    var layers = null;
    var total = 0;
    var visibilityById = {};

    function restoreLayerVisibility() {
        if (!layers) return;
        for (var i = 0; i < total; i++) {
            var layer = layers[i];
            if (visibilityById[layer.id] !== undefined) {
                layer.visible = visibilityById[layer.id];
            }
        }
    }

    try {
        if (app.documents.length === 0) {
            $.writeln('PSX_LOG|error|no open document');
            return '{"success":false,"count":0,"error":"no document"}';
        }

        var doc = app.activeDocument;
        var docName = safeName(doc.name.replace(/\.[^\.]+$/, ''));
        // 显式命名优先，否则根据设置决定是否使用文档名前缀。
        var baseName = imageName ? safeName(imageName) : (withDocName ? docName : '');
        // doc.layers 只包含文档根级图层；不递归导出组内子图层。
        layers = doc.layers;
        total = layers.length;

        var backgroundLayer = null;
        for (var b = 0; b < total; b++) {
            if (isBackgroundLayer(layers[b])) {
                backgroundLayer = layers[b];
                break;
            }
        }

        for (var v = 0; v < total; v++) {
            visibilityById[layers[v].id] = layers[v].visible;
        }

        $.writeln('PSX_LOG|info|start export top-level layers, total=' + total);
        var count = 0;
        for (var layerIndex = 0; layerIndex < total; layerIndex++) {
            var targetLayer = layers[layerIndex];
            if (isBackgroundLayer(targetLayer) || /^排除/i.test(targetLayer.name)) continue;

            // 每一轮都显式显示当前目标一级图层；否则第一轮隐藏后，后续目标仍会保持隐藏。
            for (var visibilityIndex = 0; visibilityIndex < total; visibilityIndex++) {
                var candidate = layers[visibilityIndex];
                if (candidate === targetLayer) {
                    candidate.visible = true;
                } else if (candidate !== backgroundLayer && !/^排除/i.test(candidate.name)) {
                    candidate.visible = false;
                }
            }

            var layerName = safeName(targetLayer.name);
            var stem = baseName ? baseName + '_' + layerName : layerName;
            var fileName = stem + '.' + format.toLowerCase();
            var outFile = new File(exportPath + fileName);
            if (rename) {
                var fileIndex = 1;
                while (outFile.exists) {
                    outFile = new File(exportPath + stem + '_' + fileIndex + '.' + format.toLowerCase());
                    fileIndex++;
                }
            } else if (outFile.exists && !outFile.remove()) {
                throw new Error('无法覆盖同名文件: ' + outFile.fsName);
            }

            var options;
            if (format.toLowerCase() === 'png') {
                options = new PNGSaveOptions();
            } else {
                options = new JPEGSaveOptions();
                options.quality = Math.round(12 * (quality / 100));
            }
            doc.saveAs(outFile, options, true);
            count++;
            $.writeln('PSX_LOG|info|exported top-level layer ' + count + ': ' + targetLayer.name);
        }

        $.writeln('PSX_LOG|info|top-level layers done: ' + count);
        // 结果由宿主软件展示，不在 Photoshop 内弹窗。
        return '{"success":true,"count":' + count + '}';
    } catch (e) {
        $.writeln('PSX_LOG|error|exportLayers: ' + e);
        return '{"success":false,"count":0,"error":' + jsonStr(String(e)) + '}';
    } finally {
        try {
            restoreLayerVisibility();
        } catch (restoreError) {
            $.writeln('PSX_LOG|warn|restore layer visibility failed: ' + restoreError);
        }
    }
}
__psx_result = exportLayers();
