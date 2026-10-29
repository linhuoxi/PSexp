#target photoshop
// 快速导出当前文档
// 占位符: {{EXPORT_PATH}} {{FORMAT}} {{QUALITY}}
function quickExport() {
    var exportPath = '{{EXPORT_PATH}}';
    var format = '{{FORMAT}}';
    var quality = {{QUALITY}};
    try {
        if (app.documents.length === 0) {
            $.writeln('PSX_LOG|error|no open document');
            return '{"success":false,"error":"no document"}';
        }
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
        executeAction(stringIDToTypeID('exportDocumentAsFileTypePressed'), d, DialogModes.NO);
        $.writeln('PSX_LOG|info|quick export done: ' + format + ' -> ' + exportPath);
        return '{"success":true}';
    } catch (e) {
        $.writeln('PSX_LOG|error|quickExport: ' + e);
        return '{"success":false,"error":' + jsonStr(String(e)) + '}';
    }
    function jsonStr(s) {
        s = String(s).replace(/\\/g, '\\\\').replace(/"/g, '\\"');
        return '"' + s + '"';
    }
}
__psx_result = quickExport();
