$(document).ready(function () {
    const noPolicyUrl = '/img/no-policy.jpg'; // Default policy image URL
    const noUserUrl = '/img/no-user.png'; // Default user image URL
    const MaxSizeInBytes = 5242880; // 5 MB
    let originalImageUrl = ''; // To store the original image URL
    let previousFileValue = ''; // Store previous file value to reset input if needed

    // Store the original image URL when the page is loaded
    function initializeOriginalImage() {
        const previewElement = $('#createProfileImage'); // Select the preview image element
        originalImageUrl = previewElement.attr('src'); // Store the original image URL
    }

    // Call initializeOriginalImage() to store the original image when the page is loaded
    initializeOriginalImage();

    function showAlert(title, content, type) {
        $.alert({
            title: title,
            content: content,
            icon: 'fas fa-exclamation-triangle',
            type: type,
            closeIcon: true,
            buttons: {
                cancel: {
                    text: "CLOSE",
                    btnClass: 'btn-danger'
                }
            }
        });
    }

    function resetImageInput(inputElement, previewElement, defaultImageUrl) {
        // Reset the file input to empty and keep the image preview intact
        inputElement.val(''); // Clear the input value
        if (previewElement) {
            previewElement.attr('src', defaultImageUrl || originalImageUrl); // Revert to the original image
        }
    }

    $(".document-image-input").on('change', function () {
        const inputElement = $(this);
        const previewElement = $(`#${inputElement.data('preview-id')}`); // Dynamically find preview element
        const defaultImageUrl = inputElement.data('default-image'); // Get the default image URL from the data attribute

        const files = inputElement[0].files;

        if (files.length === 0) return; // No files selected, exit

        const file = files[0];
        const fileSize = file.size;
        const fileName = file.name.toLowerCase();
        const fileExtension = fileName.substring(fileName.lastIndexOf('.') + 1);

        // Store the current file input value and preview image before making changes
        previousFileValue = inputElement.val();
        const currentPreviewUrl = previewElement.attr('src');

        // Validate file type
        if (!["gif", "png", "jpg", "jpeg"].includes(fileExtension)) {
            showAlert(
                "INVALID FILE TYPE",
                "Please select only image files with extensions: jpg, png, gif, jpeg!",
                'red'
            );
            resetImageInput(inputElement, previewElement, originalImageUrl);
            return;
        }

        // Validate file size
        if (fileSize > MaxSizeInBytes) {
            showAlert(
                "IMAGE UPLOAD ISSUE",
                "<i class='fa fa-upload'></i> Upload Image size limit exceeded. <br />Max file size is 5 MB!",
                'red'
            );
            resetImageInput(inputElement, previewElement, originalImageUrl);
            return;
        }

        // Check if FileReader is supported
        if (typeof FileReader !== "undefined") {
            // Preview the selected image
            const fileReader = new FileReader();
            fileReader.onload = function (e) {
                if (previewElement) {
                    previewElement.attr('src', e.target.result); // Set the preview image source
                }
            };
            fileReader.readAsDataURL(file);
        } else {
            showAlert(
                "OUTDATED BROWSER",
                "This browser does not support FileReader. Please use a modern browser!",
                'red'
            );
            resetImageInput(inputElement, previewElement, originalImageUrl);
        }

        // Check if the file selected is the same as the default image (noPolicyUrl or noUserUrl)
        if (fileName.includes(noPolicyUrl) || fileName.includes(noUserUrl)) {
            $.alert({
                title: "NO FILE SELECTED",
                content: "No file selected, please choose an image.",
                icon: 'fas fa-exclamation-triangle',
                type: "red",
                closeIcon: true,
                buttons: {
                    cancel: {
                        text: "CLOSE",
                        btnClass: 'btn-danger'
                    }
                }
            });
            resetImageInput(inputElement, previewElement, originalImageUrl);
        }
    });

    // To keep the previously selected image when reverting to previous value
    function revertToOriginalImage(inputElement, previewElement) {
        // Revert the file input value to empty (if needed) and set the preview to the original image
        inputElement.val(''); // Clear the input value
        previewElement.attr('src', originalImageUrl); // Set the preview image to the original image
    }

    // Example usage to revert the image if the user cancels or selects an invalid image
    $(".cancel-button").on('click', function () {
        const inputElement = $(".document-image-input");
        const previewElement = $(`#${inputElement.data('preview-id')}`);
        revertToOriginalImage(inputElement, previewElement);
    });
});
