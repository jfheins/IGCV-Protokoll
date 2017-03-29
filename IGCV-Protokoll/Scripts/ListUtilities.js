function collapseListsOut() {
	$('.panel-collapse').collapse('show');
	$('.panel-collapse-heading a').removeClass("collapsed");
}

function collapseListsIn() {
	$('.panel-collapse').collapse('hide');
	$('.panel-collapse-heading a').addClass("collapsed");
}

function ReplaceRow(list, rowId, data) {
	$('#' + list + '_' + rowId).replaceWith(data);
	RefreshTables(list);
	enableDatePicker();
	autosize($('#' + list + '_' + rowId + ' textarea'));
}

function RemoveRow(list, rowId) {
	$('#' + list + '_' + rowId).remove();
	RefreshTables(list);
}

function AddRow(list, data) {
	$('#' + list + '_tbody').append(data);
	$('#' + list + '_form')[0].reset();
	$('#' + list + '_form ~ div.alert').alert('close');
	RefreshTables(list);
}

function RefreshTables(list) {
	var selector = 'table.table-sortable';
	if (list) {
		selector = '#' + list + '_table';
	}
	$(selector).trigger('update');
	$("table.table-sortable time[rel=timeago]").timeago();
}

$(document).ready(function () {
	$('div.listcontainer').each(function() {
		var tableId = $(this).find('table').eq(0).attr('id');

		var actions = $(this).find('.actions a');
		$.each(actions, function(index, node) {
			$(this).attr("href", $(this).attr("href") + "?returnURL=" + returnURL + "%23" + tableId);
		});

	});
});



autosize($('textarea'));