$(document).ready(function () {

    const noPolicyUrl = '/img/no-policy.jpg';
    const noUserUrl = '/img/no-user.png';
    const MaxSizeInBytes = 2097152; // 2 MB
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
        inputElement.val(''); // Clear the input value
        if (previewElement) {
            previewElement.attr('src', defaultImageUrl || noUserUrl); // Reset to default image
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

        // Validate file type
        if (!["gif", "png", "jpg", "jpeg"].includes(fileExtension)) {
            showAlert(
                "INVALID FILE TYPE",
                "Please select only image files with extensions: jpg, png, gif, jpeg!",
                'red'
            );
            resetImageInput(inputElement, previewElement, defaultImageUrl);
            return;
        }

        // Validate file size
        if (fileSize > MaxSizeInBytes) {
            showAlert(
                "IMAGE UPLOAD ISSUE",
                "<i class='fa fa-upload'></i> Upload Image size limit exceeded. <br />Max file size is 2 MB!",
                'red'
            );
            resetImageInput(inputElement, previewElement, defaultImageUrl);
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
            resetImageInput(inputElement, previewElement, defaultImageUrl);
        }

        if (defaultImageUrl == noPolicyUrl || defaultImageUrl == noUserUrl) {
            $.alert({
                title: "NO FILE SELECTED",
                content: "NO FILE SELECTED",
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
        }
    });
});