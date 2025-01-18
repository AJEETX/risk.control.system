$(document).ready(function () {
    initializeSummernote();
    initializeAutocomplete();
    setupFileValidation();
    setupFormSubmitConfirmation();
});

function initializeSummernote() {
    $('#RawMessage').summernote({
        height: 300,
        focus: false
    });
    $("#receipient-email").focus();
}

function initializeAutocomplete() {
    let availableSuggestions = [];

    $("#receipient-email").autocomplete({
        source: function (request, response) {
            $("#loader").show();
            $.ajax({
                url: "/api/MasterData/GetUserBySearch",
                type: "GET",
                data: { search: request.term },
                success: function (data) {
                    availableSuggestions = data.map(item => ({ label: item, value: item }));
                    response(availableSuggestions);
                    $("#loader").hide();
                },
                error: function () {
                    availableSuggestions = [];
                    response([]);
                    $("#loader").hide();
                }
            });
        },
        minLength: 1,
        select: function (event, ui) {
            $("#receipient-email").val(ui.item.value);
        }
    });

    $("#receipient-email").on("blur", function () {
        const inputValue = $(this).val().trim();
        const isValid = availableSuggestions.some(item => item.label === inputValue);
        $(this).toggleClass("input-invalid", !isValid).val(isValid ? inputValue : "");
    });
}

function setupFileValidation() {
    const maxFileSize = 2 * 1024 * 1024; // 2MB
    const validExtensions = ["gif", "png", "jpg", "jpeg"];
    const defaultImage = '/img/no-image.png';

    $("#document").on('change', function () {
        const files = $(this)[0].files;

        for (let i = 0; i < files.length; i++) {
            const file = files[i];
            const fileSize = file.size;
            const fileExtension = file.name.split('.').pop().toLowerCase();

            if (!validExtensions.includes(fileExtension)) {
                showAlert("Invalid File Type", "Only images (jpg, png, gif) are allowed.", "red");
                resetFileInput(this);
                return;
            }

            if (fileSize > maxFileSize) {
                showAlert("File Size Exceeded", "Maximum allowed size is 2MB.", "red");
                resetFileInput(this);
                return;
            }

            // Show preview
            $("#documentImage0").attr("src", URL.createObjectURL(file));
        }
    });

    function resetFileInput(input) {
        input.value = "";
        $("#documentImage0").attr("src", defaultImage);
    }
}

function setupFormSubmitConfirmation() {
    let askConfirmation = true;

    $('#email-form').submit(function (e) {
        if (askConfirmation) {
            e.preventDefault();
            $.confirm({
                title: "Confirm Send",
                content: "Are you sure you want to send this email?",
                icon: 'far fa-envelope',
                type: 'green',
                buttons: {
                    confirm: {
                        text: "SEND",
                        btnClass: 'btn-success',
                        action: function () {
                            askConfirmation = false;
                            $('#email-form').submit();
                        }
                    },
                    cancel: {
                        text: "CANCEL",
                        btnClass: 'btn-default'
                    }
                }
            });
        }
    });
}

function showAlert(title, content, type) {
    $.alert({
        title: title,
        content: content,
        icon: 'fas fa-exclamation-triangle',
        type: type,
        buttons: {
            cancel: {
                text: "CLOSE",
                btnClass: 'btn-danger'
            }
        }
    });
}
