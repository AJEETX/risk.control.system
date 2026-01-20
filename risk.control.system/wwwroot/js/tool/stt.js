// Update file name label
$('.custom-file-input').on('change', function () {
    let fileName = $(this).val().split('\\').pop();
    $(this).next('.custom-file-label').addClass("selected").html(fileName);
});

// Copy to clipboard function
function copyToClipboard() {
    var copyText = document.getElementById("transcriptionBox");
    copyText.select();
    copyText.setSelectionRange(0, 99999);
    document.execCommand("copy");
    alert("Text copied!");
}

// Handle loading spinner
$('#sttForm').on('submit', function () {
    $('.submit-progress').removeClass('hidden');
    // 2. Select the button
    var $btn = $('#btnConvert');

    // 3. Change text and disable to prevent double clicks
    $btn.prop('disabled', true);
    $btn.html('<i class="fas fa-spinner fa-spin mr-2"></i> Transcribing...');
});