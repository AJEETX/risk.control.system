// Add Face ID functionality
document.getElementById("addFaceIdButton").addEventListener("click", function (e) {
    e.preventDefault();
    var faceIdsContainer = document.getElementById("faceIdsContainer");
    var newFaceId = document.createElement("div");
    newFaceId.classList.add("faceId-entry");
    newFaceId.innerHTML = `
                <input name="FaceIds[${faceIdsContainer.children.length}].FaceIdName" class="form-control mt-2" placeholder="Face ID Name" />
                <select name="FaceIds[${faceIdsContainer.children.length}].DigitalIdReportType" class="form-control mt-2">
                    <option value="">Select Digital ID Type</option>
                    <option value="AGENT_FACE">Agent Face</option>
                    <option value="CLIENT_FACE">Client Face</option>
                </select>
                <a href="#" class="removeFaceId btn btn-danger btn-sm">Remove</a>
            `;
    faceIdsContainer.appendChild(newFaceId);
});

// Remove Face ID entry
document.getElementById("faceIdsContainer").addEventListener("click", function (e) {
    if (e.target && e.target.classList.contains("removeFaceId")) {
        e.preventDefault();
        e.target.closest(".faceId-entry").remove();
    }
});

// Add Document ID functionality
document.getElementById("addDocumentIdButton").addEventListener("click", function (e) {
    e.preventDefault();
    var documentIdsContainer = document.getElementById("documentIdsContainer");
    var newDocumentId = document.createElement("div");
    newDocumentId.classList.add("documentId-entry");
    newDocumentId.innerHTML = `
                <input name="DocumentIds[${documentIdsContainer.children.length}].DocumentIdName" class="form-control mt-2" placeholder="Document ID Name" />
                <select name="DocumentIds[${documentIdsContainer.children.length}].DocumentIdReportType" class="form-control mt-2">
                    <option value="">Select Document ID Type</option>
                    <option value="PASSPORT">Passport</option>
                    <option value="LICENSE">License</option>
                </select>
                <a href="#" class="removeDocumentId btn btn-danger btn-sm">Remove</a>
            `;
    documentIdsContainer.appendChild(newDocumentId);
});

// Remove Document ID entry
document.getElementById("documentIdsContainer").addEventListener("click", function (e) {
    if (e.target && e.target.classList.contains("removeDocumentId")) {
        e.preventDefault();
        e.target.closest(".documentId-entry").remove();
    }
});

// Add Question functionality
document.getElementById("addQuestionButton").addEventListener("click", function (e) {
    e.preventDefault();
    var questionsContainer = document.getElementById("questionsContainer");
    var newQuestion = document.createElement("div");
    newQuestion.classList.add("question-entry");
    newQuestion.innerHTML = `
                <input name="Questions[${questionsContainer.children.length}].QuestionText" class="form-control mt-2" placeholder="Question" />
                <select name="Questions[${questionsContainer.children.length}].QuestionType" class="form-control mt-2">
                    <option value="Text">Text</option>
                    <option value="Dropdown">Dropdown</option>
                </select>
                <a href="#" class="removeQuestion btn btn-danger btn-sm">Remove</a>
            `;
    questionsContainer.appendChild(newQuestion);
});

// Remove Question entry
document.getElementById("questionsContainer").addEventListener("click", function (e) {
    if (e.target && e.target.classList.contains("removeQuestion")) {
        e.preventDefault();
        e.target.closest(".question-entry").remove();
    }
});