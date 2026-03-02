$(document).ready(function () {
    $("#Code").on("input", function (e) {
        // 1. Remove any non-numeric characters using a Regex
        this.value = this.value.replace(/[^0-9]/g, '');

        // 2. Double check length (redundant but safe for copy-paste)
        if (this.value.length > 6) {
            this.value = this.value.slice(0, 6);
        }
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
            { data: 'updated' },
            {
                data: 'pinCodeId',
                render: function (data, type, row) {
                    return `
                                <a id="edit${data}"class="btn btn-xs btn-warning" href="/Pincodes/Edit/${data}">
                                    <i class="fas fa-map-pin"></i> Edit
                                </a>
                                <button type="button" class="btn btn-xs btn-danger delete-item" data-id="${data}">
                                            <i class="fas fa-trash"></i> Delete
                                        </a>`;
                }
            }
        ],
        "drawCallback": function (setting) {
            $('#dataTable tbody').on('click', '.btn-warning', function (e) {
                e.preventDefault(); // Prevent the default anchor behavior
                var id = $(this).attr('id').replace('edit', ''); // Extract the ID from the button's ID attribute
                showedit(id); // Call the getdetails function with the ID
                window.location.href = $(this).attr('href'); // Navigate to the delete page
            });
        }
    });

    var askConfirmation = true;
    $('#create-form').submit(function (e) {
        if (askConfirmation) {
            if ($('#create-form').valid()) { // Ensure `valid` is called as a method
                e.preventDefault();
                $.confirm({
                    title: "Confirm  Add New",
                    content: "Are you sure to add?",

                    icon: 'fas fa-map-pin',
                    type: 'green',
                    closeIcon: true,
                    buttons: {
                        confirm: {
                            text: " Add New",
                            btnClass: 'btn-success',
                            action: function () {
                                askConfirmation = false;
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
                                $('#create').html("<i class='fas fa-sync fa-spin' aria-hidden='true'></i> Add New");

                                $('#create-form').submit();
                                var form = document.getElementById("create-form");
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
                    title: 'Invalid detail',
                    content: 'Complete the required fields',
                    type: 'red'
                })
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
                    title: 'Invalid detail',
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

var state = $('#SelectedStateId').val();
var district = $('#SelectedDistrictId').val();
if (state != '0' && district != '0') {
    $('#Name').focus();
} else {
    $('#StateId').focus();
}