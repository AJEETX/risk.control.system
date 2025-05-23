document.getElementById("addFaceIdButton").addEventListener("click", function () {
    var faceIdsContainer = document.getElementById("faceIdsContainer");
    var newFaceId = document.createElement("div");
    if (faceIdsContainer && newFaceId) {
        newFaceId.classList.add("faceId-entry", "d-flex", "justify-content-between", "align-items-center");
        const digitalIdReportTypes = document.getElementById('digitalIdReportTypes').value.split(',');
        // Start creating select options
        let optionsHtml = `<option value="">Select Digital ID Type</option>`;
        digitalIdReportTypes.forEach(function (type) {
            optionsHtml += `<option value="${type}">${type}</option>`;
        });
        newFaceId.innerHTML = `
        <div class="d-flex w-100">
            <input name="FaceIds[${faceIdsContainer.children.length}].FaceIdName" class="form-control mt-2" placeholder="Face ID Name" />
            <select name="FaceIds[${faceIdsContainer.children.length}].DigitalIdReportType" class="form-control mt-2">
                ${optionsHtml}
            </select>
            </div>
            <button type="button" class="btn btn-outline-danger delete-question-btn btn-sm mt-2 deleteFaceIdButton text-sm"><i class="fa fa-trash"> </i></button> <!-- ADD DELETE BUTTON HERE -->
        `;


        faceIdsContainer.appendChild(newFaceId);
    }
});

document.getElementById("addDocumentIdButton").addEventListener("click", function () {
    var documentIdsContainer = document.getElementById("documentIdsContainer");
    var newDocumentId = document.createElement("div");
    if (documentIdsContainer && newDocumentId) { 
        newDocumentId.classList.add("documentId-entry", "d-flex", "justify-content-between", "align-items-center");
        const documentIdReportTypes = document.getElementById('documentIdReportTypes').value.split(',');
        let optionsHtml = `<option value="">Select Document ID Type</option>`;
        documentIdReportTypes.forEach(function (type) {
            optionsHtml += `<option value="${type}">${type}</option>`;
        });
        newDocumentId.innerHTML = `
        <div class="d-flex w-100">
            <input name="DocumentIds[${documentIdsContainer.children.length}].DocumentName" class="form-control mt-2" placeholder="Document Name" />
            <select name="DocumentIds[${documentIdsContainer.children.length}].DocumentIdReportType" class="form-control mt-2">
               ${optionsHtml}
            </select>
            </div>
            <button type="button" class="btn btn-outline-danger delete-question-btn btn-sm mt-2 deleteDocumentIdButton text-sm"><i class="fa fa-trash"> </i></button> <!-- <<< ADD THIS -->
    `;

        documentIdsContainer.appendChild(newDocumentId);
    }
});

document.addEventListener('click', function (event) {
    if (event.target.classList.contains('deleteFaceIdButton')) {
        event.preventDefault();
        const faceIdEntry = event.target.closest('.faceId-entry');
        if (faceIdEntry) {
            console.log(faceIdEntry);
            faceIdEntry.remove();
        }
    }
});

document.addEventListener('click', function (event) {
    if (event.target.classList.contains('deleteDocumentIdButton')) {
        event.preventDefault();
        const faceIdEntry = event.target.closest('.documentId-entry');
        if (faceIdEntry) {
            console.log(faceIdEntry);
            faceIdEntry.remove();
        }
    }
});