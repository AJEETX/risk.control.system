document.querySelectorAll('.drop-zone').forEach(zone => {
    const input = zone.querySelector('input[type="file"]');
    const preview = zone.querySelector('img');
    const text = zone.querySelector('p');

    // Click to open file dialog
    zone.addEventListener('click', () => input.click());

    // Handle file selection via dialog
    input.addEventListener('change', () => {
        if (input.files.length) updatePreview(input.files[0], preview, text);
    });

    // Drag and drop events
    ['dragenter', 'dragover'].forEach(event => {
        zone.addEventListener(event, (e) => {
            e.preventDefault();
            zone.classList.add('bg-secondary', 'text-white');
        });
    });

    ['dragleave', 'drop'].forEach(event => {
        zone.addEventListener(event, (e) => {
            e.preventDefault();
            zone.classList.remove('bg-secondary', 'text-white');
        });
    });

    zone.addEventListener('drop', (e) => {
        const files = e.dataTransfer.files;
        if (files.length) {
            input.files = files; // Assign files to the hidden input
            updatePreview(files[0], preview, text);
        }
    });
});

function updatePreview(file, previewElement, textElement) {
    if (file.type.startsWith('image/')) {
        const reader = new FileReader();
        reader.onload = (e) => {
            previewElement.src = e.target.result;
            previewElement.classList.remove('d-none');
            textElement.innerHTML = `<span class="text-success">Ready: ${file.name}</span>`;
        };
        reader.readAsDataURL(file);
    }
}

function readURL(input, previewId) {
    if (input.files && input.files[0]) {
        var reader = new FileReader();
        reader.onload = function (e) {
            $(previewId).attr('src', e.target.result).removeClass('d-none');
        }
        reader.readAsDataURL(input.files[0]);
    }
}

$("#OriginalFaceImage").change(function () { readURL(this, '#previewOriginal'); });
$("#MatchFaceImage").change(function () { readURL(this, '#previewMatch'); });

// AJAX Submission
$("#faceMatchForm").on("submit", function (e) {
    e.preventDefault();

    const formData = new FormData(this);
    const $btn = $("#btnSubmit");
    const $spinner = $("#btnSpinner");
    const $resultDiv = $("#resultContainer");

    // UI Feedback
    $btn.prop("disabled", true);
    $spinner.removeClass("d-none");
    $resultDiv.addClass("d-none");

    $.ajax({
        url: '/FaceMatch/FaceMatch',
        type: 'POST',
        data: formData,
        processData: false,
        contentType: false,
        success: function (data) {
            $("#jsonResult").text(JSON.stringify(data, null, 2));
            $resultDiv.removeClass("d-none");
        },
        error: function (xhr) {
            alert("Error: " + xhr.responseText);
        },
        complete: function () {
            $btn.prop("disabled", false);
            $spinner.addClass("d-none");
        }
    });
});