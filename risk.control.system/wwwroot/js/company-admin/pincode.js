$(document).ready(function () {
    $("#Code").on("input", function (e) {
        // 1. Remove any non-numeric characters using a Regex
        this.value = this.value.replace(/[^0-9]/g, '');

        // 2. Double check length (redundant but safe for copy-paste)
        if (this.value.length > 6) {
            this.value = this.value.slice(0, 6);
        }
    });

    var isChecking = false; // Flag to prevent recursion

    $("#Code").on("blur", function () {
        var code = $(this).val().trim().toUpperCase();
        if (!code || isChecking) return;

        isChecking = true; // Block further triggers

        var id = $('#SelectedDistrictId').val() || null;
        var CountryId = $('#CountryId').val();
        var StateId = $('#SelectedStateId').val();
        var DistrictId = $('#SelectedDistrictId').val();
        $.ajax({
            url: '/Pincodes/CheckDuplicateCode',
            type: 'POST',
            data: {
                __RequestVerificationToken: $('input[name="__RequestVerificationToken"]').val(),
                code: code,
                id: id,
                CountryId: CountryId,
                StateId: StateId,
                DistrictId: DistrictId
            },
            success: function (exists) {
                if (exists) {
                    $.alert({
                        title: '<i class="fas fa-exclamation-triangle text-danger"></i> Duplicate Pincode!',
                        content: 'The Pincode <b>' + code + '</b> already exists. Please choose another.',
                        type: 'red',
                        buttons: {
                            ok: {
                                action: function () {
                                    $("#Code").val("").focus();
                                    $("#Code").addClass('is-invalid').removeClass('is-valid');
                                }
                            }
                        },
                        // 2. THIS IS THE KEY: Wait for the modal to fully close
                        onClose: function () {
                            setTimeout(function () {
                                $("#Code").focus();
                            }, 400); // 100ms delay ensures the modal is gone
                            $("#Code").addClass('is-invalid').removeClass('is-valid');
                        }
                    });
                } else {
                    $("#Code").removeClass('is-invalid').addClass('is-valid');
                }
            },
            error: function () {
                isChecking = false;
                $.alert({
                    title: 'Error',
                    content: 'Error occurred checking if State Code already exist.',
                    type: 'red',
                });
            }
        });
    });

    $('#dataTable').DataTable({
        processing: true,
        serverSide: true,
        ajax: {
            url: '/Pincodes/GetPincodes',
            type: 'GET',
            dataType: 'json',
            data: function (d) {
                d.search = d.search.value; // Pass the search term
                d.orderColumn = d.order[0].column; // Column index (0, 1, 2, etc.)
                d.orderDirection = d.order[0].dir; // Sorting direction ("asc" or "desc")
            }
        },
        order: [[0, 'asc']],
        fixedHeader: true,
        processing: true,
        paging: true,
        language: {
            loadingRecords: '&nbsp;',
            processing: '<i class="fas fa-sync fa-spin fa-4x fa-fw"></i><span class="sr-only">Loading...</span>'
        },
        columns: [
            { data: 'code' },
            { data: 'name' },
            { data: 'district' },
            { data: 'state' },
            { data: 'country' },
            { data: 'updatedBy' },
            {
                "data": "updated",
                "render": function (data, type, row) {
                    if (!data) return '';
                    let date = new Date(data);
                    var localDate = date.toLocaleString('en-IN', {
                        day: '2-digit',
                        month: 'short',
                        year: 'numeric',
                        hour: '2-digit',
                        minute: '2-digit',
                        second: '2-digit',
                        hour12: true
                    });
                    return `<span title="Updated time: ${localDate}" data-bs-toggle="tooltip"><small><strong>${localDate}</strong></small></span>`;
                }
            },
            {
                data: 'pinCodeId',
                bSortable: false,
                render: function (data, type, row) {
                    return `
                                <a data-id="${data}" class="btn btn-xs btn-warning">
                                    <i class="fas fa-map-pin"></i> Edit
                                </a>
                                <button type="button" class="btn btn-xs btn-danger delete-item" data-id="${data}">
                                            <i class="fas fa-trash"></i> Delete
                                        </a>`;
                }
            }
        ],
        "drawCallback": function (settings, start, end, max, total, pre) {
            // Reinitialize Bootstrap 5 tooltips
            var tooltipTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="tooltip"]'));
            tooltipTriggerList.map(function (el) {
                return new bootstrap.Tooltip(el, {
                    html: true,
                    sanitize: false   // ⬅⬅⬅ THIS IS THE FIX
                });
            });
        }
    });
    $('body').on('click', 'a.btn-xs.btn-warning', function (e) {
        e.preventDefault();
        const id = $(this).data('id');
        showdetail(id, this);
    });
    function showdetail(id, element) {
        id = String(id).replace(/[^a-zA-Z0-9_-]/g, "");
        $("body").addClass("submit-progress-bg");
        setTimeout(() => $(".submit-progress").removeClass("hidden"), 1);

        showSpinnerOnButton(element, "Edit");

        const url = `/Pincodes/Edit/${encodeURIComponent(id)}`;

        setTimeout(() => {
            window.location.href = url;
        }, 1000);
    }
    function showSpinnerOnButton(selector, spinnerText) {
        $(selector).html(`<i class='fas fa-sync fa-spin'></i> ${spinnerText}`);
    }
    var askConfirmation = true;

    $('#create-form').on('submit', function (e) {
        var $form = $(this);

        if (askConfirmation) {
            if ($form.valid()) {
                e.preventDefault(); // Stop the initial click

                $.confirm({
                    title: "Confirm Add New",
                    content: "Are you sure you want to add this pincode?",
                    icon: 'fas fa-map-pin',
                    type: 'green',
                    buttons: {
                        confirm: {
                            text: "Add New",
                            btnClass: 'btn-success',
                            action: function () {
                                askConfirmation = false; // Set flag to allow the next submit

                                // Show progress UI
                                $("body").addClass("submit-progress-bg");
                                $(".submit-progress").removeClass("hidden");

                                // Only disable the button, NOT the whole form/inputs
                                $('#create').prop('disabled', true)
                                    .html("<i class='fas fa-sync fa-spin'></i> Submitting...");

                                // Trigger the native DOM submit (bypasses jQuery)
                                $form[0].submit();
                            }
                        },
                        cancel: {
                            text: "Cancel",
                            btnClass: 'btn-default'
                        }
                    }
                });
            } else {
                $.alert({
                    title: 'Incomplete Details',
                    content: 'Please fill in all required fields.',
                    type: 'red'
                });
            }
        }
    });

    var askEditConfirmation = true;
    $('#edit-form').submit(function (e) {
        if (askEditConfirmation) {
            if ($('#edit-form').valid()) {
                e.preventDefault();
                $.confirm({
                    title: "Confirm Edit",
                    content: "Are you sure to edit?",
                    icon: 'fas fa-map-pin',

                    type: 'orange',
                    closeIcon: true,
                    buttons: {
                        confirm: {
                            text: "Edit ",
                            btnClass: 'btn-warning',
                            action: function () {
                                askEditConfirmation = false;
                                $("body").addClass("submit-progress-bg");
                                // Wrap in setTimeout so the UI
                                // can update the spinners
                                setTimeout(function () {
                                    $(".submit-progress").removeClass("hidden");
                                }, 1);
                                // Disable all buttons, submit inputs, and anchors
                                $('button, input[type="submit"], a').prop('disabled', true);

                                // Add a class to visually indicate disabled state for anchors
                                $('a').addClass('disabled-anchor').on('click', function (e) {
                                    e.preventDefault(); // Prevent default action for anchor clicks
                                });
                                $('#edit').html("<i class='fas fa-sync fa-spin' aria-hidden='true'></i> Edit ");
                                $('#edit-form').submit();
                                var form = document.getElementById("edit-form");
                                if (form) {
                                    const formElements = form.getElementsByTagName("*");
                                    for (const element of formElements) {
                                        element.disabled = true;
                                        if (element.hasAttribute("readonly")) {
                                            element.classList.remove("valid", "is-valid", "valid-border");
                                            element.removeAttribute("aria-invalid");
                                        }
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
            else {
                $.alert({
                    title: 'Incomplete detail',
                    content: 'Complete the required fields',
                    type: 'red'
                })
            }
        }
    });

    $('a.create').on('click', function () {
        $("body").addClass("submit-progress-bg");
        // Wrap in setTimeout so the UI
        // can update the spinners
        setTimeout(function () {
            $(".submit-progress").removeClass("hidden");
        }, 1);
        // Disable all buttons, submit inputs, and anchors
        $('button, input[type="submit"], a').prop('disabled', true);

        // Add a class to visually indicate disabled state for anchors
        $('a').addClass('disabled-anchor').on('click', function (e) {
            e.preventDefault(); // Prevent default action for anchor clicks
        });

        $('a.create').html("<i class='fas fa-sync fa-spin'></i> Add New");

        var article = document.getElementById("article");
        if (article) {
            var nodes = article.getElementsByTagName('*');
            for (var i = 0; i < nodes.length; i++) {
                nodes[i].disabled = true;
            }
        }
    });

    $(document).on("click", ".delete-item", function () {
        var $btn = $(this);
        var $spinner = $(".submit-progress"); // global spinner (you already have this)
        var id = $(this).data("id");
        var row = $(this).closest("tr");
        var table = $('#dataTable').DataTable();

        $.confirm({
            title: 'Confirm Deletion',
            content: 'Are you sure you want to delete ?',
            type: 'red',
            buttons: {
                confirm: {
                    text: 'Yes, Delete',
                    btnClass: 'btn-red',
                    action: function () {
                        $spinner.removeClass("hidden");
                        $btn.prop("disabled", true).html('<i class="fas fa-sync fa-spin"></i> Delete');
                        $.ajax({
                            url: '/Pincodes/Delete',
                            type: 'POST',
                            data: {
                                __RequestVerificationToken: $('input[name="__RequestVerificationToken"]').val(),
                                id: id
                            },
                            success: function (response) {
                                if (response.success) {
                                    table.row(row).remove().draw(false); // correct DataTable removal
                                    $.alert({
                                        title: 'Deleted',
                                        content: response.message,
                                        type: 'red'
                                    });
                                } else {
                                    $.alert(response.message);
                                }
                            },
                            error: function (e) {
                                $.alert('Error while deleting.');
                            },
                            complete: function () {
                                $spinner.addClass("hidden");
                                // ✅ Re-enable button and restore text
                                $btn.prop("disabled", false).html('<i class="fas fa-trash"></i> Delete');
                            }
                        });
                    }
                },
                cancel: {
                    text: 'Cancel',
                    btnClass: 'btn-default'
                }
            }
        });
    });
    var countryId = $("#SelectedCountryId").val();
    var selectedStateId = $("#SelectedStateId").val();
    var selectedDistrictId = $("#SelectedDistrictId").val();
    if (countryId && countryId !== "0") {
        loadStates(countryId, selectedStateId);
    }
    function loadStates(countryId, selectedStateId = null) {
        $("#StateId").empty();
        $("#StateId").append('<option value="">Loading...</option>');

        $.ajax({
            url: '/District/GetStatesByCountry',
            type: 'GET',
            data: { countryId: countryId },
            success: function (data) {
                $("#StateId").empty();
                $("#StateId").append('<option value="">-- Select State --</option>');

                $.each(data, function (i, state) {
                    $("#StateId").append(
                        $('<option>', {
                            value: state.id,
                            text: state.name
                        })
                    );
                });

                // Set selected state (for Edit scenario)
                if (selectedStateId) {
                    $("#StateId").val(selectedStateId);
                    loadDistrictData(selectedStateId, countryId);
                }
                $("#StateId").focus();
            },
            error: function () {
                $("#StateId").empty();
                $("#StateId").append('<option value="">Error loading states</option>');
            }
        });
    }

    $('#StateId').on('change', function () {
        var selectedVal = $(this).val();
        $("#SelectedStateId").val(selectedVal);
        $("#DistrictId").empty();

        $('#Name').val('');
        $('#Code').val('');
        loadDistrictData($(this).val(), countryId);
    });
    $('#DistrictId').on('change', function () {
        var selectedVal = $(this).val();
        $("#SelectedDistrictId").val(selectedVal);
        $('#Name').val('');
        $('#Code').val('');
    });

    function loadDistrictData(stateId, countryId) {
        $("#DistrictId").empty();
        $("#DistrictId").append('<option value="">Loading...</option>');
        $.ajax({
            url: '/Pincodes/GetDistrictsByStatesAndCountry',
            type: 'GET',
            data: { countryId: countryId, stateId: stateId },
            success: function (data) {
                $("#DistrictId").empty();
                $("#DistrictId").append('<option value="">-- Select District --</option>');
                if (data.length > 0) {
                    $.each(data, function (i, district) {
                        $("#DistrictId").append(
                            $('<option>', {
                                value: district.id,
                                text: district.name
                            })
                        );
                    });
                    // Set selected state (for Edit scenario)
                    if (selectedStateId && selectedDistrictId != '0') {
                        $("#DistrictId").val(selectedDistrictId);
                    }
                    $("#StateId").focus();
                }
            },
            error: function () {
                $("#DistrictId").empty();
                $("#DistrictId").append('<option value="">Error loading district</option>');
            }
        });
    }
});

function showedit(id) {
    $("body").addClass("submit-progress-bg");
    // Wrap in setTimeout so the UI
    // can update the spinners
    setTimeout(function () {
        $(".submit-progress").removeClass("hidden");
    }, 1);

    $('a#edit' + id + '.btn.btn-xs.btn-warning').html("<i class='fas fa-sync fa-spin'></i> Edit");

    disableAllInteractiveElements();

    var article = document.getElementById("article");
    if (article) {
        var nodes = article.getElementsByTagName('*');
        for (var i = 0; i < nodes.length; i++) {
            nodes[i].disabled = true;
        }
    }
}

var state = $('#StateId').val();
if (state) {
    state.focus();
}