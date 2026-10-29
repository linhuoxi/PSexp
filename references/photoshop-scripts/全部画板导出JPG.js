$$#target photoshop

// 设置固定导出文件夹路径
var exportFolder = "{导出路径}";

// 导出当前文档
quick_export_jpg(exportFolder);

function quick_export_jpg(path, layer) {
    try {
        if (layer == undefined) layer = false;    

        var d = new ActionDescriptor();
        var r = new ActionReference();
        
        // 设置导出目标为当前图层
        r.putEnumerated(stringIDToTypeID("layer"), stringIDToTypeID("ordinal"), stringIDToTypeID("targetEnum"));
        d.putReference(stringIDToTypeID("null"), r);
        
        // 设置文件类型为 JPG
        d.putString(stringIDToTypeID("fileType"), "jpg");
        d.putInteger(stringIDToTypeID("quality"), {图片质量}); // 设置 JPG 质量，范围 0-100
        d.putInteger(stringIDToTypeID("metadata"), 0); // 不包含元数据
        d.putString(stringIDToTypeID("destFolder"), path); // 目标文件夹路径
        d.putBoolean(stringIDToTypeID("sRGB"), true); // 使用 sRGB 色彩空间
        d.putBoolean(stringIDToTypeID("openWindow"), false); // 不打开导出窗口

        // 执行导出操作
        executeAction(stringIDToTypeID(layer ? "exportSelectionAsFileTypePressed" : "exportDocumentAsFileTypePressed"), d, DialogModes.NO);
    } catch (e) {
        throw (e);
    }
}
