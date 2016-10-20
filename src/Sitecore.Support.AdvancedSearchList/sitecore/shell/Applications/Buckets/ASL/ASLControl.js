
window.ASLC = window.ASLC || {};

ASLC.refreshValue = function (selectedField, valueInput) {

    var value = "";

    for (var n = 0; n < selectedField.options.length; n++) {
        var option = selectedField.options[n];
        value += (value != "" ? "|" : "") + option.value;
    }

    valueInput.value = value;
}

ASLC.deleteCurrent = function (fieldId) {
    var selectedFieldId = "select#" + fieldId + "_selected";
    var selectedField = $sc(selectedFieldId);

    var selectedOptions = selectedField.children("option:selected");
    selectedField = selectedField[0];

    if (selectedOptions.length > 0) {
        var index = selectedOptions.index();
        selectedOptions.remove();

        if (selectedField.length > 0) {
            index = Math.min(Math.max(--index, 0), selectedField.length - 1);
        }

        selectedField.selectedIndex = index;
    }

    var valueId = "#" + fieldId + "_Value";
    var valueInput = $sc(valueId);
    ASLC.refreshValue(selectedField, valueInput[0]);

    selectedField.focus();
}

ASLC.openCurrent = function (fieldId) {
    var selectedFieldId = "select#" + fieldId + "_selected";
    var selectedField = $sc(selectedFieldId);

    var selectedOptions = selectedField.children("option:selected");

    if (selectedOptions.length > 0) {
        var id = selectedOptions[0].value;
        window.scForm.postRequest('', '', '', 'contenteditor:launchtab(url=' + id + ')');
    }
}
