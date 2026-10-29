#target photoshop

var v = checkArtboards();

function checkArtboards() {
    var abList = listArtboards();
    return abList.length > 0;
}

function listArtboards() {
    var result = [];
    try {
        var docRef = new ActionReference();
        docRef.putEnumerated(charIDToTypeID("Dcmn"), charIDToTypeID("Ordn"), charIDToTypeID("Trgt"));
        var appDesc = executeActionGet(docRef);
        var numOfLayers = appDesc.getInteger(stringIDToTypeID("numberOfLayers"));

        for (var i = 1; i <= numOfLayers; i++) {
            var layerRef = new ActionReference();
            layerRef.putIndex(charIDToTypeID("Lyr "), i);
            var layerDesc = executeActionGet(layerRef);
            if (layerDesc.hasKey(stringIDToTypeID("artboardEnabled")) && layerDesc.getBoolean(stringIDToTypeID("artboardEnabled"))) {
                result.push(layerDesc.getInteger(stringIDToTypeID('layerID')));
            }
        }
    } catch (e) {
        $.writeln("EXCEPTION: ", e);
    }
    return result;
}

v;
