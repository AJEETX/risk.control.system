$(document).ready(function () {

    $('.textarea-editor').summernote({
        height: 350, // set editor height
        minHeight: null, // set minimum height of editor
        maxHeight: null, // set maximum height of editor
        focus: true // set focus to editable area after initializing summernote
    });

    $('tbody tr').on('click', function () {
        let id = $(this).data('url');
        if (typeof id !== 'undefined') {
            window.location.href = id;
        }
    });

    // delete messages
    $('#delete-messages').on('click', function () {
        let ids = [];
        let form = $('#listForm');
        let checkboxArray = document.getElementsByName('ids');

        // check if checkbox is checked
        for (let i = 0; i < checkboxArray.length; i++) {
            if (checkboxArray[i].checked)
                ids.push(checkboxArray[i].value);
        }

        // submit form
        if (ids.length > 0) {
            if (confirm("Are you sure you want to delete this item(s)?")) {
                form.submit();
            }
        }
    });
    $('#reply-form').on('click', '#sendReply', function () {
        let txtArea = $('#replyMessage');
        let msgLength = txtArea.val().length;

        if (msgLength === 0) {
            if (confirm('Send this message without text in the body?')) {
                $('#reply-form').submit();
            } else {
                return;
            }
        } else {
            $('#reply-form').submit();
        }
    });

    $('#reply-form').on('click', '#show-reply', function () {
        let container = $(this).parent().parent();
        container.empty();

        let template = `
                <div>
                    <h5>Reply to: @Model.Email</h5>
                    <textarea id="replyMessage" name="Reply" style="width: 100%;" rows="7"></textarea>
                    <button type="button" id="sendReply" class ="btn btn-primary">Send</button>
                    <button type="button" id="cancelReply" class ="btn btn-danger">Cancel</button>
                </div>`;

        container.append(template);
    });

    $('#reply-form').on('click', '#cancelReply', function () {
        let container = $(this).parent().parent();
        container.empty();
        container.append('<div class="text-muted">click <span id="show-reply" class="clickable-text">here</span> to reply</div>');
    });

    $('#delete-message').on('click', function () {
        $('#deleteForm').submit();
    });


    // check all or uncheck all
    $('.checkall-toggle').on('click', function () {
        var clicks = $(this).data('clicks');

        if (clicks) {
            $('input[type="checkbox"]').iCheck('uncheck');
            $('.fa', this).removeClass('fa-check-square-o').addClass('fa-square-o');
            $('.fa', this).text(' Check all');
        } else {
            $('input[type="checkbox"]').iCheck('check');
            $('.fa', this).removeClass('fa-square-o').addClass('fa-check-square-o');
            $('.fa', this).text(' Uncheck all');
        }

        $(this).data('clicks', !clicks);
    });

    //// iCheck
    //$('input').iCheck({
    //    checkboxClass: 'icheckbox_square-blue',
    //    radioClass: 'iradio_square-blue',
    //    increaseArea: '20%' // optional
    //});



    /** add active class and stay opened when selected */
    var url = window.location;
    const allLinks = document.querySelectorAll('.nav-item a');
    const currentLink = [...allLinks].filter(e => {
        return e.href == url;
    });

    if (currentLink.length > 0) { //this filter because some links are not from menu
        currentLink[0].classList.add("active");
        if (currentLink[0].closest(".nav-treeview")) {
            currentLink[0].closest(".nav-treeview").style.display = "block";
        }
    }
    // Attach the call to toggleChecked to the
    // click event of the global checkbox:
    $("#checkall").click(function () {
        var status = $("#checkall").prop('checked');
        $('#manage-vendors').prop('disabled', !status)
        toggleChecked(status);
    });

    $("input.vendors").click(function () {
        //var status = $(this).prop('checked');

        //$(this).prop('checked', status);
        //$('#manage-vendors').prop('disabled', !status);

        var checkboxes = $("input[type='checkbox'].vendors");
        var anyChecked = checkIfAnyChecked(checkboxes);
        var allChecked = checkIfAllChecked(checkboxes);
        $('#checkall').prop('checked', allChecked);
        $('#manage-vendors').prop('disabled', !anyChecked)
    });

    $("#btnDeleteImage").click(function () {
        var id = $(this).attr("data-id");
        $.ajax({
            url: '/User/DeleteImage/' + id,
            type: "POST",
            async: true,
            success: function (data) {
                if (data.succeeded) {
                    $("#delete-image-main").hide();
                    $("#ProfilePictureUrl").val("");
                }
                else {
                   // toastr.error(data.message);
                }
            },
            beforeSend: function () {
                $(this).attr("disabled", true);
                $(this).html('<span class="spinner-border spinner-border-sm" role="status" aria-hidden="true"></span> Loading...');
            },
            complete: function () {
                $(this).html('Delete Image');
            },
        });
    });
});

function checkIfAllChecked(elements) {
    var totalElmentCount = elements.length;
    var totalCheckedElements = elements.filter(":checked").length;
    return (totalElmentCount == totalCheckedElements)
}

function checkIfAnyChecked(elements) {
    var hasAnyCheckboxChecked = false;

    $.each(elements, function (index, element) {
        if (element.checked === true) {
            hasAnyCheckboxChecked= true;
        }
    });
    return hasAnyCheckboxChecked;
}
function loadState(obj, showDefaultOption = true) {
    var value = obj.value;
    $.post("/User/GetStatesByCountryId", { countryId: value }, function (data) {
        PopulateStateDropDown("#PinCodeId", "#DistrictId", "#StateId", data, "<option>--SELECT STATE--</option>", "<option>--SELECT DISTRICT--</option>", "<option>--SELECT PINCODE--</option>", showDefaultOption);
    });
}
function loadDistrict(obj, showDefaultOption = true) {
    var value = obj.value;
    $.post("/User/GetDistrictByStateId", { stateId: value }, function (data) {
        PopulateDistrictDropDown("#PinCodeId", "#DistrictId", data, "<option>--SELECT PINCODE--</option>", "<option>--SELECT DISTRICT--</option>", showDefaultOption);
    });
}
function loadPinCode(obj, showDefaultOption= true) {
    var value = obj.value;
    $.post("/User/GetPinCodesByDistrictId", { districtId: value }, function (data) {
        PopulatePinCodeDropDown("#PinCodeId", data, "<option>--SELECT PINCODE--</option>", showDefaultOption);
    });
}

function loadSubStatus(obj) {
    var value = obj.value;
    $.post("/InvestigationCaseSubStatus/GetSubstatusBystatusId", { InvestigationCaseStatusId: value }, function (data) {
        PopulateSubStatus("#InvestigationCaseSubStatusId", data, "<option>--SELECT SUB STATUS--</option>");
    });
}

function PopulateSubStatus(dropDownId, list, option) {
    $(dropDownId).empty();
    $(dropDownId).append(option)
    $.each(list, function (index, row) {
        $(dropDownId).append("<option value='" + row.investigationServiceTypeId + "'>" + row.code + "</option>")
    });
}

function loadInvestigationServices(obj) {
    var value = obj.value;
    $.post("/VendorService/GetInvestigationServicesByLineOfBusinessId", { LineOfBusinessId: value }, function (data) {
        PopulateInvestigationServices("#InvestigationServiceTypeId", data, "<option>--SELECT TYPE OF INVESTIGATION--</option>");
    });
}
function PopulateInvestigationServices(dropDownId, list, option) {
    $(dropDownId).empty();
    $(dropDownId).append(option)
    $.each(list, function (index, row) {
        $(dropDownId).append("<option value='" + row.investigationServiceTypeId + "'>" + row.code + "</option>")
    });
}
function PopulateDistrictDropDown(pinCodedropDownId, districtDropdownId, list, pincodeOption, districtOption, showDefaultOption) {
    $(pinCodedropDownId).empty();
    if (showDefaultOption){
        $(pinCodedropDownId).append(pincodeOption)
    }

    $(districtDropdownId).empty();
    $(districtDropdownId).append(districtOption)

    $.each(list, function (index, row) {
        $(districtDropdownId).append("<option value='" + row.districtId + "'>" + row.name + "</option>")
    });
}
function PopulatePinCodeDropDown(dropDownId, list, option, showDefaultOption) {
    $(dropDownId).empty();
    if (showDefaultOption)
    $(dropDownId).append(option)
    $.each(list, function (index, row) {
        $(dropDownId).append("<option value='" + row.pinCodeId + "'>" + row.name + " -- " + row.code + "</option>")
    });
}
function PopulateStateDropDown(pinCodedropDownId, districtDropDownId, stateDropDownId, list, stateOption, districtOption, pincodeOption, showDefaultOption) {
    $(stateDropDownId).empty();
    $(districtDropDownId).empty();
    $(pinCodedropDownId).empty();

    $(stateDropDownId).append(stateOption);
    $(districtDropDownId).append(districtOption);
    if (showDefaultOption) {
        $(pinCodedropDownId).append(pincodeOption);
    }

    $.each(list, function (index, row) {
        $(stateDropDownId).append("<option value='" + row.stateId + "'>" + row.name + "</option>")
    });
}
function toggleChecked(status) {
    $("#checkboxes input").each(function () {
        // Set the checked status of each to match the 
        // checked status of the check all checkbox:
        $(this).prop("checked", status);        
    });
}
function readURL(input) {
    var url = input.value;
    var ext = url.substring(url.lastIndexOf('.') + 1).toLowerCase();
    if (input.files && input.files[0] && (ext == "gif" || ext == "png" || ext == "jpeg" || ext == "jpg" || ext == "csv")) {
        var reader = new FileReader();

        reader.onload = function (e) {
            $('#img').attr('src', e.target.result);
        }

        reader.readAsDataURL(input.files[0]);
    } else {
        $('#img').attr('src', '/img/no-image.png');
    }
}