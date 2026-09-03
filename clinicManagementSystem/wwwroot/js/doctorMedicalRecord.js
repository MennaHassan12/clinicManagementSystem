function addRow() {
    var list = document.getElementById("prescription-list");
    if (!list) return;

    var idx = list.getElementsByClassName("prescription-item").length;

    var div = document.createElement("div");
    div.className = "row g-2 mb-2 prescription-item align-items-center";
    div.innerHTML =
        '<div class="col-md-3"><input name="Prescriptions[' + idx + '].MedicineName" class="form-control" placeholder="Medicine Name" /></div>' +
        '<div class="col-md-2"><input name="Prescriptions[' + idx + '].Dosage" class="form-control" placeholder="Dosage (500mg)" /></div>' +
        '<div class="col-md-2"><input name="Prescriptions[' + idx + '].Frequency" class="form-control" placeholder="Frequency (3x Daily)" /></div>' +
        '<div class="col-md-2"><input name="Prescriptions[' + idx + '].Duration" class="form-control" placeholder="Duration (7 Days)" /></div>' +
        '<div class="col-md-2"><input name="Prescriptions[' + idx + '].Instructions" class="form-control" placeholder="Instructions" /></div>' +
        '<div class="col-md-1"><button type="button" onclick="removeRow(this)" class="btn btn-outline-danger btn-sm rounded-circle">✖</button></div>';

    list.appendChild(div);
}

function removeRow(btn) {
    var list = document.getElementById("prescription-list");
    if (list.getElementsByClassName("prescription-item").length > 1) {
        btn.closest(".prescription-item").remove();
        reIndexRows();
    }
}

function reIndexRows() {
    var list = document.getElementById("prescription-list");
    var rows = list.getElementsByClassName("prescription-item");

    for (var i = 0; i < rows.length; i++) {
        var inputs = rows[i].getElementsByTagName("input");
        for (var j = 0; j < inputs.length; j++) {
            var name = inputs[j].getAttribute("name");
            if (name) {
                inputs[j].setAttribute("name", name.replace(/\[\d+\]/, "[" + i + "]"));
            }
        }
    }
}