#target photoshop
// 导出 PS 当前选中的图层/画板
// 原理：尊重用户在 PS 里的当前选择，直接调用 exportSelectionAsFileTypePressed
// 跟原版"选择画板导出JPG.js / 选择画板导出PNG.js"逻辑一致
// 占位符: {{EXPORT_PATH}} {{FORMAT}} {{QUALITY}} {{RENAME}}
function exportSelectedArtboards() {
    function jsonStr(s) {
        s = String(s).replace(/\\/g, '\\\\').replace(/"/g, '\\"');
        return '"' + s + '"';
    }
    var exportPath = '{{EXPORT_PATH}}';
    var format = '{{FORMAT}}';
    var quality = {{QUALITY}};
    var rename = {{RENAME}}; // true / false（当前由 PS 快速导出自动处理重名）
    // PNG 固定用 32（与原版"选择画板导出PNG.js"一致）
    if (format.toLowerCase() === 'png') quality = 32;
    try {
        if (app.documents.length === 0) {
            $.writeln('PSX_LOG|error|no open document');
            return '{"success":false,"count":0,"error":"no document"}';
        }

        // 导出前统计目录已有文件数（用于计算新增数 + 等待异步落盘）
        var folderBefore = new Folder(exportPath);
        var filesBefore = folderBefore.getFiles('*.' + format.toLowerCase());
        var beforeCount = filesBefore.length;

        $.writeln('PSX_LOG|info|start export selection as ' + format + ' (PS current selection)');

        // 跟原版一致：layer reference + exportSelectionAsFileTypePressed
        var d = new ActionDescriptor();
        var r = new ActionReference();
        r.putEnumerated(stringIDToTypeID('layer'), stringIDToTypeID('ordinal'), stringIDToTypeID('targetEnum'));
        d.putReference(stringIDToTypeID('null'), r);
        d.putString(stringIDToTypeID('fileType'), format.toLowerCase());
        d.putInteger(stringIDToTypeID('quality'), quality);
        d.putInteger(stringIDToTypeID('metadata'), 0);
        d.putString(stringIDToTypeID('destFolder'), exportPath);
        d.putBoolean(stringIDToTypeID('sRGB'), true);
        d.putBoolean(stringIDToTypeID('openWindow'), false);
        executeAction(stringIDToTypeID('exportSelectionAsFileTypePressed'), d, DialogModes.NO);

        // 快速导出是异步的：轮询等待文件落盘（最多 20 秒）
        var folderAfter = new Folder(exportPath);
        var filesAfter = folderAfter.getFiles('*.' + format.toLowerCase());
        var afterCount = filesAfter.length;
        var waited = 0;
        while (afterCount <= beforeCount && waited < 200) {
            $.sleep(100);
            waited++;
            filesAfter = folderAfter.getFiles('*.' + format.toLowerCase());
            afterCount = filesAfter.length;
        }
        var newCount = afterCount - beforeCount;
        $.writeln('PSX_LOG|info|selection done, new files: ' + newCount + ', total in folder: ' + afterCount + ', waited ' + waited + 'x100ms');
        return '{"success":true,"count":' + newCount + '}';
    } catch (e) {
        $.writeln('PSX_LOG|error|exportSelectedArtboards: ' + e);
        return '{"success":false,"count":0,"error":' + jsonStr(String(e)) + '}';
    }
}
__psx_result = exportSelectedArtboards();
