// Please see documentation at https://docs.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.
jQuery(function ()
{
    $("#search_inp").on("keyup", function () {
        var value = $(this).val().toLowerCase();
        $("#tuser-tbl tbody tr").filter(function () {
            $(this).toggle($(this).text().toLowerCase().indexOf(value) > -1);
        });
        $("#form_list option").filter(function () {
            $(this).toggle($(this).text().toLowerCase().indexOf(value) > -1);
        });
    });
    $('#Periodic').on('change', function (event) {
        var periodic = $(event.target).val();
        $.ajax({
            async: true,
            type: 'GET',
            url: '/regulations/index?periodic=' + periodic,
            success: function (data) {
                $('#Regulations').empty();
                // Заполняем список регламентов новыми данными
                $.each(data, function (index, item) {
                    $('#Regulations').append($('<option>').val(item.value).text(item.text));
                });
            },
            error: function (XMLHttpRequest) {
                $.notify("Ошибка /Form/Check. Статус - " + XMLHttpRequest.status + " Техт: " + XMLHttpRequest.responseText, {
                    clickToHide: false,
                    autoHideDelay: 10000
                });
            }
        }).then(function (data) {

        });
    });
    $('#form_list').on('change', function (event) {
        var periodic = $(event.target).val();
        $.ajax({
            async: true,
            type: 'GET',
            url: '/regulations/index?periodic=' + periodic,
            success: function (data) {
                $('#Regulations').empty();
                // Заполняем список регламентов новыми данными
                $.each(data, function (index, item) {
                    $('#Regulations').append($('<option>').val(item.value).text(item.text));
                });
            },
            error: function (XMLHttpRequest) {
                $.notify("Ошибка /Form/Check. Статус - " + XMLHttpRequest.status + " Техт: " + XMLHttpRequest.responseText, {
                    clickToHide: false,
                    autoHideDelay: 10000
                });
            }
        }).then(function (data) {

        });
    });
//    $(document).ready(function () {
        $('input[type="checkbox"]').prop('disabled', true);
        $('input[type="checkbox"]').prop('checked', false);

        // Обработчик изменения выпадающего списка
        $('#FormKind').on('change',function () {
            // Получить выбранное значение
            var selectedValue = $(this).val();

            // Сбросить состояние всех чекбоксов
            $('input[type="checkbox"]').prop('disabled', true);
            $('input[type="checkbox"]').prop('checked', false);

            if (selectedValue === '1') {
                $('#HasSubject').prop('checked', true).prop('disabled', true);
            }
            else if (selectedValue === '2') {
                $('#HasSubject, #HasFacility').prop('disabled', false)
            }
            else if (selectedValue === '3') {
                $('#HasSubject, #HasFacility').prop('disabled', true).prop('checked', true);
            }
            else if (selectedValue === '4') {
                $('#HasTerritory').prop('disabled', true).prop('checked', true);
            }
            else {
                $('input[type="checkbox"]').prop('disabled', true);
            }
        });
//    });

});
