document.addEventListener("click", function (e) {
    if (e.target.classList.contains("btn-clear-date")) {
        const input = e.target.closest(".position-relative").querySelector("input[type='date']");
        if (input) {
            input.value = ""; // Clear the value
            input.blur();     // Remove focus to hide calendar
        }
    }
});


document.addEventListener("keydown", function (event) {
    if (event.key === "Escape") {
        const activeElement = document.activeElement;
        if (activeElement && activeElement.type === "date") {
            activeElement.blur(); // Closes the date picker
        }
    }
});

document.querySelectorAll(".delete-question-btn").forEach(btn => {
    btn.addEventListener("click", () => {
        if (document.activeElement && document.activeElement.type === "date") {
            document.activeElement.blur();
        }
    });
});

document.addEventListener("click", function (event) {
    const activeElement = document.activeElement;

    // Check if the active element is a date input
    if (activeElement && activeElement.type === "date") {
        // Check if the click was outside of that input
        if (!activeElement.contains(event.target)) {
            activeElement.blur(); // This will close the date picker
        }
    }
});

document.addEventListener("DOMContentLoaded", function () {
    const questionType = document.getElementById("questionType");
    const optionsGroup = document.getElementById("optionsGroup");
    if (questionType && optionsGroup) {
        function toggleOptions() {
            const shouldShow = questionType.value === "dropdown" || questionType.value === "radio";
            optionsGroup.classList.toggle("hidden", !shouldShow);
        }

        questionType.addEventListener("change", toggleOptions);
        toggleOptions(); // Run once on load
    }
});

document.querySelectorAll('.delete-question-btn').forEach(button => {
    button.addEventListener('click', function () {
        var questionId = this.getAttribute('data-question-id');
        if (questionId) {
            // Use jConfirm instead of native confirm
            $.confirm({
                title: 'Confirm Deletion',
                content: 'Are you sure you want to delete this question?',
                buttons: {
                    confirm: {
                        text: 'Yes',
                        type: 'red',
                        btnClass: 'btn-danger',
                        icon: 'fas fa-trash',
                        action: function () {
                            fetch("/Question/DeleteQuestion", {
                                method: "POST",
                                headers: {
                                    "Content-Type": "application/json",
                                },
                                credentials: "include", // Include cookies in the request
                                body: JSON.stringify({ id: questionId })
                            })
                                .then(res => {
                                    if (res.ok) {
                                        // Remove the entire question row
                                        var questionDiv = $('#question-' + questionId);
                                        if (questionDiv && questionDiv.length) {
                                            questionDiv.slideUp(400, function () {
                                                questionDiv.remove();

                                                // Check if any questions remain
                                                if ($('[id^=question-]').length === 0) {
                                                    $('#submit-answer').remove(); // Remove the submit button

                                                    // Optionally show a message
                                                    $('<div class="d-flex justify-content-center align-items-center"><div class="alert alert-light bg-white border shadow-sm text-center px-4 py-3 rounded w-100">No CLAIM Investigation questions available at the moment.</div></div>')
                                                        .appendTo('#alert-no-question')
                                                        .fadeIn();
                                                }

                                                $.alert('Question deleted successfully.');
                                            });
                                        }
                                    } else {
                                        $.alert("Delete failed.");
                                    }
                                });
                        }
                    },
                    cancel: {
                        text: 'No',
                        action: function () {
                            // Do nothing on cancel
                        }
                    }
                }
            });
        }
       
    });
});
let faceIdIndex = 0;
let documentIdIndex = 0;
var faceId = document.getElementById("add-faceid");
if (faceId) {
    faceId.addEventListener("click", function () {
        const container = document.getElementById("faceids-container");
        const html = `
            <div class="card p-2 mt-2">
                <h5>FaceId ${faceIdIndex + 1}</h5>

                <div class="form-group">
                    <label>FaceId Type</label>
                    <select name="FaceIds[${faceIdIndex}].ReportType" class="form-control">
                        <option value="0">AGENT_FACE</option>
                        <option value="1">SINGLE_FACE</option>
                        <option value="2">DUAL_FACE</option>
                        <option value="3">HOUSE_FRONT</option>
                    </select>
                </div>

                <div class="form-group">
                    <label>Upload Image (optional)</label>
                    <input type="file" name="FaceIds[${faceIdIndex}].IdImage" class="form-control" />
                </div>
            </div>
        `;
        container.insertAdjacentHTML('beforeend', html);
        faceIdIndex++;
    });
}
var docId = document.getElementById("add-documentid");
if (docId) {
    docId.addEventListener("click", function () {
        const container = document.getElementById("documentids-container");
        const html = `
            <div class="card p-2 mt-2">
                <h5>DocumentId ${documentIdIndex + 1}</h5>

                <div class="form-group">
                    <label>Document Type</label>
                    <select name="DocumentIds[${documentIdIndex}].DocumentIdReportType" class="form-control">
                        <option value="0">ADHAAR</option>
                        <option value="1">PAN</option>
                        <option value="2">DRIVING_LICENSE</option>
                        <option value="3">PASSPORT</option>
                    </select>
                </div>

                <div class="form-group">
                    <label>Upload Document Image (optional)</label>
                    <input type="file" name="DocumentIds[${documentIdIndex}].IdImage" class="form-control" />
                </div>
            </div>
        `;
        container.insertAdjacentHTML('beforeend', html);
        documentIdIndex++;
    });

}

$.validator.setDefaults({
    submitHandler: function (form) {
        $.confirm({
            title: "Confirm SUBMIT",
            content: "Are you sure to SUBMIT?",
            icon: 'fa fa-question',

            closeIcon: true,
            type: 'green',
            buttons: {
                confirm: {
                    text: "SUBMIT",
                    btnClass: 'btn-success',
                    action: function () {
                        askConfirmation = false;
                        $("body").addClass("submit-progress-bg");
                        // Wrap in setTimeout so the UI
                        // can update the spinners
                        setTimeout(function () {
                            $(".submit-progress").removeClass("hidden");
                        }, 1);

                        $('#submit-answer').html("<i class='fas fa-sync fa-spin' aria-hidden='true'></i> Submit Answers");
                        disableAllInteractiveElements();

                        form.submit();
                        var createForm = document.getElementById("answers");
                        if (createForm) {

                            var nodes = createForm.getElementsByTagName('*');
                            for (var i = 0; i < nodes.length; i++) {
                                nodes[i].disabled = true;
                            }
                        }
                    }
                },
                cancel: {
                    text: "Cancel",
                    btnClass: 'btn-default'
                }
            }
        });
    }
});
$(document).ready(function () {
    $("#answers").validate();
});