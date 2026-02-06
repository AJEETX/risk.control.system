document.addEventListener("DOMContentLoaded", function () {
    // Reference to the modal and close button
    var termsModal = document.getElementById('termsModal');
    var closeTermsButton = document.getElementById('closeterms');
    // Select all elements with the class 'termsLink'
    var termsLinks = document.querySelectorAll('.termsLink');

    // Add a click event listener to each element
    if (termsLinks) {
        termsLinks.forEach(function (termsLink) {
            termsLink.addEventListener('click', function (e) {
                e.preventDefault(); // Prevent default link behavior (i.e., not navigating anywhere)

                // Show the terms modal
                var termsModal = document.querySelector('#termsModal');
                termsModal.classList.remove('hidden-section');
                termsModal.classList.add('show');
            });
        });
    }
    // Close the modal when clicking the close button
    if (closeTermsButton) {
        closeTermsButton.addEventListener('click', function () {
            termsModal.classList.add('hidden-section'); // Remove the 'show' class to hide the modal
            termsModal.classList.remove('show'); // Remove the 'show' class to hide the modal
        });
    }

    // Optionally, you can close the modal if clicked outside the modal content
    window.addEventListener('click', function (e) {
        if (e.target === termsModal) {
            termsModal.classList.add('hidden-section'); // Remove the 'show' class to hide the modal
            termsModal.classList.remove('show'); // Close the modal if clicked outside
        }
    });
});

$(document).ready(function () {
    var caseId = $('#caseId').val();

    var latitude = "";
    var longitude = "";
    if (navigator.geolocation) {
        navigator.geolocation.getCurrentPosition(success, error);
    } else {
        alert("Geolocation is not supported by your browser.");
    }

    function success(position) {
        var coordinates = position.coords;
        $('#latitude').val(coordinates.latitude);
        $('#longitude').val(coordinates.longitude);

        latitude = (coordinates.latitude);
        longitude = (coordinates.longitude);
    }

    function error(err) {
        console.warn(`ERROR(${err.code}): ${err.message}`);
        alert("Unable to retrieve your location. Please allow location access in your browser.");
    }

    $(".upload-face-btn").click(function () {
        const button = $(this);
        var $row = button.closest("tr"); // get current row
        var faceId = button.data("faceid");
        var fileInput = $row.find(".face-upload")[0]; // only search inside this row
        const statusDiv = $(`#upload-status-${faceId}`);
        const faceImage = $(`#face-img-${faceId}`);
        const reportName = button.data("name");
        const locationName = button.data("location-name");

        faceImage.addClass("loading-effect");
        const locationId = button.data("locationid");

        var file = fileInput.files[0];
        if (!file) {
            alert("Please select an image to upload.");
            return;
        }

        var formData = new FormData();
        formData.append("Id", faceId);
        formData.append("Image", file);
        formData.append("latitude", latitude);
        formData.append("longitude", longitude);
        formData.append("caseId", caseId);
        formData.append("locationId", locationId);

        var isAgent = button.data("isagent") === true || button.data("isagent") === "true";
        formData.append("reportName", reportName);
        formData.append("locationName", locationName);
        formData.append("isAgent", isAgent);

        // Get anti-forgery token
        var token = $('input[name="__RequestVerificationToken"]').val();
        formData.append("__RequestVerificationToken", token);
        // Show uploading indicator
        statusDiv.html('<span class="text-info"><i class="fas fa-spinner fa-spin"></i> Uploading...</span>');
        button.prop("disabled", true);

        $.ajax({
            url: '/AgentReport/SubmitFaceImage',
            type: "POST",
            data: formData,
            processData: false,
            contentType: false,
            headers: {
                __RequestVerificationToken: $('input[name="__RequestVerificationToken"]').val(),
            },
            success: function (response) {
                if (response.success) {
                    statusDiv.html('<span class="text-success">Upload successful.</span>');

                    // ✅ Update base64 image
                    const base64Image = `data:image/*;base64,${response.image}`;
                    faceImage.attr("src", base64Image);
                    faceImage.on('load', function () {
                        faceImage.removeClass("loading-effect");
                    });
                    // ✅ Reset file input
                    $(fileInput).val('');
                } else {
                    statusDiv.html('<span class="text-danger">Upload failed.</span>');
                }
            },
            error: function () {
                statusDiv.html('<span class="text-danger">Upload failed.</span>');
            },
            complete: function () {
                button.prop("disabled", false);
                setTimeout(() => statusDiv.html(''), 3000);
            }
        });
    });

    $(".upload-doc-btn").click(function () {
        const button = $(this);
        var docId = button.data("docid");
        var fileInput = $(".doc-upload[data-docid='" + docId + "']")[0];
        const statusDiv = $(`#doc-upload-status-${docId}`);
        const docImage = $(`#doc-img-${docId}`);
        docImage.addClass("loading-effect");
        const locationId = button.data("locationid");
        const reportName = button.data("name");
        const locationName = button.data("location-name");

        var file = fileInput.files[0];
        if (!file) {
            alert("Please select a document image to upload.");
            return;
        }

        var formData = new FormData();
        formData.append("Id", docId);
        formData.append("Image", file);
        formData.append("latitude", latitude);
        formData.append("longitude", longitude);
        formData.append("caseId", caseId);
        formData.append("locationId", locationId);
        formData.append("reportName", reportName);
        formData.append("locationName", locationName);
        // Get anti-forgery token
        var token = $('input[name="__RequestVerificationToken"]').val();
        formData.append("__RequestVerificationToken", token);
        statusDiv.html('<span class="text-info"><i class="fas fa-spinner fa-spin"></i> Uploading...</span>');
        button.prop("disabled", true);

        $.ajax({
            url: '/AgentReport/SubmitDocumentImage',
            type: "POST",
            data: formData,
            processData: false,
            contentType: false,
            success: function (response) {
                if (response.success) {
                    statusDiv.html('<span class="text-success">Upload successful.</span>');

                    // ✅ Update base64 image
                    const base64Image = `data:image/*;base64,${response.image}`;
                    docImage.attr("src", base64Image);
                    docImage.on('load', function () {
                        docImage.removeClass("loading-effect");
                    });
                    // ✅ Reset file input
                    $(fileInput).val('');
                } else {
                    statusDiv.html('<span class="text-danger">Upload failed.</span>');
                }
            },
            error: function () {
                statusDiv.html('<span class="text-danger">Upload failed.</span>');
            },
            complete: function () {
                button.prop("disabled", false);
                setTimeout(() => statusDiv.html(''), 3000);
            }
        });
    });

    $(".upload-media-btn").click(function () {
        const button = $(this);
        var docId = button.data("docid");
        var fileInput = $(".media-upload[data-docid='" + docId + "']")[0];
        const statusDiv = $(`#media-upload-status-${docId}`);
        const docImage = $(`#doc-img-${docId}`);
        docImage.addClass("loading-effect");
        const reportName = button.data("name");
        const locationName = button.data("location-name");

        var file = fileInput.files[0];
        if (!file) {
            alert("Please select a document image to upload.");
            return;
        }

        var formData = new FormData();
        formData.append("Id", docId);
        formData.append("Image", file);
        formData.append("latitude", latitude);
        formData.append("longitude", longitude);
        formData.append("caseId", caseId);
        formData.append("reportName", reportName);
        formData.append("locationName", locationName);
        // Get anti-forgery token
        var token = $('input[name="__RequestVerificationToken"]').val();
        formData.append("__RequestVerificationToken", token);
        statusDiv.html('<span class="text-info"><i class="fas fa-spinner fa-spin"></i> Uploading...</span>');
        button.prop("disabled", true);
        const allowedTypes = [
            'video/mp4',
            'video/webm',
            'audio/mpeg',
            'audio/wav',
            'audio/aac',
            'audio/mp4',
            'audio/x-aac',
            'audio/vnd.dlna.adts'
        ];

        if (!allowedTypes.includes(file.type)) {
            alert("Unsupported file format. Please upload MP4, WebM, MP3, AAC, or WAV.");
            return;
        }
        console.log("Uploaded file type:", file.type);

        $.ajax({
            url: '/AgentReport/SubmitMediaFile',
            type: "POST",
            data: formData,
            processData: false,
            contentType: false,
            success: function (response) {
                if (response.success) {
                    statusDiv.html('<span class="text-success">Upload successful.</span>');
                    let mediaType = response.extension.includes('mp4') || response.extension.includes('webm') ? 'video' : 'audio';
                    const mediaTag = `
                        <${mediaType} controls>
                            <source src="data:${mediaType}/${response.extension};base64,${response.fileData}" type="${mediaType}/${response.extension}">
                            Your browser does not support the ${mediaType} tag.
                        </${mediaType}>
                    `;
                    docImage.replaceWith(`<div id="doc-img-${docId}">${mediaTag}</div>`);
                    $(fileInput).val('');
                } else {
                    statusDiv.html('<span class="text-danger">Upload failed.</span>');
                }
            },
            error: function (err) {
                statusDiv.html('<span class="text-danger">Upload failed.</span>');
            },
            complete: function () {
                button.prop("disabled", false);
                setTimeout(() => statusDiv.html(''), 3000);
            }
        });
    });
    function toggleSubmitButton() {
        var report = $('#remarks').val().trim();
        var isChecked = $('#terms_and_conditions').is(':checked');

        if (isChecked && report !== '') {
            $('#submit-case').attr("disabled", false);
            $('#questionaire').css('background-color', 'green');
            $('#questionaire-border').addClass('border-success');
        } else {
            $('#submit-case').attr("disabled", true);
            $('#questionaire').css('background-color', 'grey');
            $('#questionaire-border').removeClass('border-success');
        }
    }

    // Bind the function to both events
    $('#remarks').on('input', toggleSubmitButton);
    $('#terms_and_conditions').on('change', toggleSubmitButton);

    // Call it once on page load to ensure correct initial state
    toggleSubmitButton();
    let askConfirmation = false;

    $('#create-form').on('submit', function (e) {
        var report = $('#remarks').val();

        if (report == '') {
            e.preventDefault();
            $.alert({
                title: "Report submission !!!",
                content: "Please enter remarks ?",
                icon: 'fas fa-exclamation-triangle',

                type: 'red',
                closeIcon: true,
                buttons: {
                    cancel: {
                        text: "OK",
                        btnClass: 'btn-danger',
                        action: function () {
                            $.alert('Canceled!');
                            $('#remarks').focus();
                        }
                    }
                }
            });
        }
        else if (!askConfirmation) {
            e.preventDefault();
            $.confirm({
                title: "Confirm SUBMIT",
                content: "Are you sure?",
                icon: 'fa fa-binoculars',

                type: 'red',
                closeIcon: true,
                buttons: {
                    confirm: {
                        text: "SUBMIT",
                        btnClass: 'btn-danger',
                        action: function () {
                            askConfirmation = true;

                            $("body").addClass("submit-progress-bg");
                            // Wrap in setTimeout so the UI
                            // can update the spinners
                            setTimeout(function () {
                                $(".submit-progress").removeClass("hidden");
                            }, 1);

                            $('#submit-case').html("<i class='fas fa-sync fa-spin'></i> SUBMIT");
                            disableAllInteractiveElements();

                            $('#create-form').submit();

                            var article = document.getElementById("article");
                            if (article) {
                                var nodes = article.getElementsByTagName('*');
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
});