﻿$(document).ready(function () {
    var table = $("#customerTable").DataTable({
        ajax: {
            url: '/api/Company/AllCompanies',
            dataSrc: ''
        },
        columnDefs: [
            {
                className: 'max-width-column-name', // Apply the CSS class,
                targets: 2                      // Index of the column to style
            },
            {
                className: 'max-width-column', // Apply the CSS class,
                targets: 5                      // Index of the column to style
            },
            {
                className: 'max-width-column-name', // Apply the CSS class,
                targets: 10                      // Index of the column to style
            }],
        order: [[13, 'desc'], [14, 'desc']], // Sort by `isUpdated` and `lastModified`,
        fixedHeader: true,
        processing: true,
        paging: true,
        language: {
            loadingRecords: '&nbsp;',
            processing: '<i class="fas fa-sync fa-spin fa-4x fa-fw"></i><span class="sr-only">Loading...</span>'
        },
        columns: [
            /* Name of the keys from
            data file source */
            {
                "data": "id", "name": "Id", "bVisible": false
            },
            {
                "sDefaultContent": "",
                "bSortable": false,
                "mRender": function (data, type, row) {
                    var img = '<img alt="' + row.name + '" title="' + row.name + '" src="' + row.document + '" class="doc-profile-image" data-toggle="tooltip"/>';
                    return img;
                }
            },
            {
                "data": "domain",
                bSortable: false,
                "mRender": function (data, type, row) {
                    return '<span title="' + row.name + '" data-toggle="tooltip">' + data + '</span>'
                }
            },
            { "data": "code" },
            { "data": "phone" },
            {
                "data": "address",
                bSortable: false,
                "mRender": function (data, type, row) {
                    return '<span title="' + row.address + '" data-toggle="tooltip">' + data + '</span>'
                }
            },
            { "data": "district" },
            { "data": "state" },
            { "data": "country" },
            {
                "sDefaultContent": "",
                "bSortable": false,
                "mRender": function (data, type, row) {
                    var buttons = "";
                    buttons += '<span class="checkbox">'
                    if (row.active == 'ACTIVE') {
                        buttons += '<i class="fa fa-toggle-on"></i>';
                    } else {
                        buttons += '<i class="fa fa-toggle-off"></i>';
                    }
                    buttons += '</span>'
                    return buttons;
                }
            },
            { "data": "updated" },
            {
                "data": "updatedBy",
                bSortable: false,
                "mRender": function (data, type, row) {
                    return '<span title="' + row.updatedBy + '" data-toggle="tooltip">' + data + '</span>'
                }
            },
            {
                "sDefaultContent": "",
                "bSortable": false,
                "mRender": function (data, type, row) {
                    var buttons = "";
                    buttons += '<a id=detail' + row.id + ' onclick="showdetails(' + row.id + ')" href="/ClientCompany/Details?Id=' + row.id + '" class="btn btn-xs btn-info"><i class="fa fa-search"></i> Detail</a>&nbsp;'
                    //buttons += '<a id=edit' + row.id + ' onclick="showedit(' + row.id + ')"  href="/ClientCompany/Edit?Id=' + row.id + '"  class="btn btn-xs btn-warning"><i class="fas fa-pen"></i> Edit</a>&nbsp;'
                    buttons += '<a id=delete' + row.id + ' onclick="getdetails(' + row.id + ')" href="/ClientCompany/Delete?Id=' + row.id + '"  class="btn btn-xs btn-danger"><i class="fas fa-trash"></i></i> Delete</a>'
                    return buttons;
                }
            },
            {
                "data": "isUpdated",
                "bVisible": false
            },
            {
                "data": "lastModified",
                bVisible: false
            }
        ],
        error: function (xhr, status, error) { alert('err ' + error) }
    });
    table.on('draw', function () {
        table.rows().every(function () {
            var data = this.data(); // Get row data
            console.log(data); // Debug row data

            if (data.isUpdated) { // Check if the row should be highlighted
                var rowNode = this.node();

                // Highlight the row
                $(rowNode).addClass('highlight-new-user');

                // Scroll the row into view
                rowNode.scrollIntoView({ behavior: 'smooth', block: 'center' });

                // Optionally, remove the highlight after a delay
                setTimeout(function () {
                    $(rowNode).removeClass('highlight-new-user');
                }, 3000);
            }
        });
    });
    $('#customerTable').on('draw.dt', function () {
        $('[data-toggle="tooltip"]').tooltip({
            animated: 'fade',
            placement: 'top',
            html: true
        });
    });
});

function getdetails(id) {
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
    $('a#delete' + id + '.btn.btn-xs.btn-danger').html("<i class='fas fa-sync fa-spin'></i> Delete");

    var tbl = document.getElementById("customerTable");
    if (tbl) {
        var nodes = tbl.getElementsByTagName('*');
        for (var i = 0; i < nodes.length; i++) {
            nodes[i].disabled = true;
        }
    }
    
}
function showdetails(id) {
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
    $('a#detail' + id + '.btn.btn-xs.btn-info').html("<i class='fas fa-sync fa-spin'></i> Detail");

    var tbl = document.getElementById("customerTable");
    if (tbl) {
        var nodes = tbl.getElementsByTagName('*');
        for (var i = 0; i < nodes.length; i++) {
            nodes[i].disabled = true;
        }
    }
}
function showedit(id) {
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
    $('a#edit ' +id + '.btn.btn-warning').html("<i class='fas fa-sync fa-spin'></i> Edit");

    var tbl = document.getElementById("customerTable");
    if (tbl) {
        var nodes = tbl.getElementsByTagName('*');
        for (var i = 0; i < nodes.length; i++) {
            nodes[i].disabled = true;
        }
    }
}