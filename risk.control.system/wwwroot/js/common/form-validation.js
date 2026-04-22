$(document).ready(function () {
    $("#DateOfIncident, #ContractIssueDate").on("change", function () {
        var issueDate = new Date($("#ContractIssueDate").val());
        var incidentDate = new Date($("#DateOfIncident").val());

        if (issueDate > incidentDate) {
            $("#DateOfIncident, #ContractIssueDate").addClass("input-validation-error is-invalid");
        } else {
            $("#DateOfIncident, #ContractIssueDate").removeClass("input-validation-error is-invalid");
        }
    });
    $('#PhoneNumber, #PinCodeId').on("cut copy paste", function (e) {
        e.preventDefault();
    });
    //$('.input-validation-error').each(function () {
    //    $(this).addClass('is-invalid'); // Boostrap compatibility

    //    // Find the parent input-group and add a red border if preferred
    //    $(this).closest('.input-group').css('border', '1px solid #dc3545');
    //});
    // Call validateInput with selectors and patterns
    validateInput('#emailAddress', /[^a-z-]/g); // Allow only alphabet characters (no spaces)
    validateInput('#ContractNumber, #IFSCCode, #Code', /[^a-zA-Z0-9]/g); // Allow only alphanumeric characters (no spaces)
    validateInput('#CauseOfLoss, #Description, #Addressline, #Comments', /[^a-zA-Z0-9 .,-]/g); // Allow alphanumeric, spaces, comma, dot, dash
    validateInput('#CustomerName, #BeneficiaryName, #StateId, #DistrictId, #FirstName, #LastName, #Name, #Branch, #BankName', /[^a-zA-Z ]/g); // Allow alphabets and spaces
    validateInput('#PhoneNumber, #BankAccountNumber, #ISDCode,#mobile', /[^0-9]/g); // Allow numeric only no spaces

    // Allow only numbers and a single dot
    validateInput('#SumAssuredValue, #Price', /[^0-9.]/g, function (value) {
        return value.replace(/(\..*)\./g, '$1'); // Prevent multiple dots
    });

    // Example usage: Validate file input for allowed types (.jpg, .png, .pdf)
    validateFileInput('#createImageInput', ['jpg', 'png', 'jpeg']); // Adjust the selector and extensions as per your needs

    // Common selector for required fields and specific inputs
    const requiredFieldsSelector = '#emailAddress, #mailAddress, #domainAddress, select[required], input[required], input[type="file"], input[type="checkbox"], #Comments, #PinCodeId';

    // Add event handlers for 'change' and 'blur' events
    $(requiredFieldsSelector).on('input change', function () {
        // Check form completion for the specified forms
        checkFormCompletion('#create-form', true);
        checkFormCompletion('#edit-form');
    });

    // Initially check the form when the page loads
    checkFormCompletion('#create-form', true);
    checkFormCompletion('#edit-form');
});


function checkFormCompletion(formSelector, create = false) {
    let isFormComplete = true;
    const $form = $(formSelector);
    var elements = create ? 'select[required], input[required], input[type="file"]' : 'select[required], input[required]';
    // Check all required fields (select, input fields, and file inputs)
    $form.find(elements).each(function () {
        const $el = $(this);

        // Check if empty
        if (!$el.val()) {
            isFormComplete = false;
            return false;
        }

        // NEW: Check if the element has been flagged as invalid by jQuery Validate
        if ($el.hasClass('input-validation-error') || $el.hasClass('is-invalid')) {
            isFormComplete = false;
            return false;
        }
    });

    // Enable or disable the submit button
    $(formSelector).find('button[type="submit"]').prop('disabled', !isFormComplete);
}
function validateFileInput(inputElement, allowedExtensions) {
    var MaxSizeInBytes = 5242880;
    if (!inputElement.files || !inputElement.files[0]) {
        return false; // Exit early if no files are selected
    }

    const file = inputElement.files[0];
    var fileSize = file.size;

    const fileExtension = file.name.split('.').pop().toLowerCase();

    if (!allowedExtensions.includes(fileExtension)) {
        inputElement.value = ''; // Clear the input

        $.alert({
            title: "FILE UPLOAD TYPE !!",
            content: `Pls select only image with extension ${allowedExtensions.join(', ')} ! `,
            icon: 'fas fa-exclamation-triangle',
            type: 'red',
            closeIcon: true,
            buttons: {
                cancel: {
                    text: "CLOSE",
                    btnClass: 'btn-danger'
                }
            }
        });
    }
    if (fileSize > MaxSizeInBytes) {
        document.getElementById('document-Image').src = '/img/no-image.png';
        document.getElementById('createImageInput').value = '';
        $.alert({
            title: "Image UPLOAD issue !",
            content: " <i class='fa fa-upload'></i> Upload Image size limit exceeded. <br />Max file size is 5 MB!",
            icon: 'fas fa-exclamation-triangle',
            type: 'red',
            closeIcon: true,
            buttons: {
                cancel: {
                    text: "CLOSE",
                    btnClass: 'btn-danger'
                }
            }
        });
    } else {
        document.getElementById('document-Image').src = window.URL.createObjectURL(file);
    }
    return true;
}
function validateInput(selector, regex) {
    $(selector).on('input', function () {
        const value = $(this).val();
        // Remove invalid characters directly using the regex
        $(this).val(value.replace(regex, ''));
    });
}