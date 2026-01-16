$(document).ready(function () {
    $('#ttsForm').on('submit', function () {
        $('#btnConvert').html('<i class="fas fa-spinner fa-spin mr-2"></i> Converting...').addClass('disabled');
    });
});
