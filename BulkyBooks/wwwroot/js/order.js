var dataTable;

$(document).ready(function () {
    var url = window.location.search;
    if (url.includes("inprocess")) {
        loadDataTable("GetOrdersList?status=inprocess");
    }
    else {
        if (url.includes("pending")) {
            loadDataTable("GetOrdersList?status=pending");
        }
        else {
            if (url.includes("completed")) {
                loadDataTable("GetOrdersList?status=completed");
            }
            else {
                if (url.includes("rejected")) {
                    loadDataTable("GetOrdersList?status=rejected");
                }
                else {
                    loadDataTable("GetOrdersList?status=all");
                }
            }
        }
    }    
});

function loadDataTable(url) {
    dataTable = $('#tblData').DataTable({
        "ajax": {
            "url": "/Admin/Order/" + url
        },
        "columns": [
            { "data": "id", "width": "10%" },
            { "data": "name", "width": "15%" },
            { "data": "phoneNumber", "width": "10%" },
            { "data": "applicationUser.email", "width": "15%" },
            { "data": "orderStatus", "width": "15%" },
            { "data": "paymentStatus", "width": "15%" },
            { "data": "orderTotal", "width": "10%" },
            {
                "data": "id",
                "render": function (data) {
                    return `
                            <div class="text-center">
                                <a href="/Admin/Order/Details/${data}" class="btn btn-success text-white" style="cursor: pointer">
                                    <i class="fas fa-edit"></i>
                                </a>
                            </div>
                        `;
                }, "width": "10%"
            }
        ]
    });
}
