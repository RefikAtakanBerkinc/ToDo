// Create this file in your wwwroot/js folder
// This file should be named todo-dragdrop.js

function initializeDragAndDrop() {
    // Only add the event listeners once
    if (window.dragDropInitialized) return;

    let draggingElement = null;

    // Find all drag handles
    document.querySelectorAll('.drag-handle').forEach(handle => {
        handle.addEventListener('dragstart', function (e) {
            // Get the todo item (parent of the drag handle)
            draggingElement = this.closest('.todo-item');

            // Set data to be used during drop
            e.dataTransfer.setData('text/plain', draggingElement.id);

            // Add dragging style
            setTimeout(() => {
                draggingElement.classList.add('dragging');
            }, 0);
        });

        handle.addEventListener('dragend', function () {
            if (draggingElement) {
                draggingElement.classList.remove('dragging');
                draggingElement = null;
            }
        });
    });

    // Make todo items droppable
    document.querySelectorAll('.todo-item').forEach(item => {
        item.addEventListener('dragover', function (e) {
            e.preventDefault();
            this.classList.add('drag-over');
        });

        item.addEventListener('dragleave', function () {
            this.classList.remove('drag-over');
        });

        item.addEventListener('drop', function (e) {
            e.preventDefault();
            this.classList.remove('drag-over');

            // Get the dragged element's id
            const draggedId = e.dataTransfer.getData('text/plain');
            const draggedElement = document.getElementById(draggedId);

            if (draggedElement && draggedElement !== this) {
                // Extract IDs from the HTML element IDs
                const draggedTodoId = parseInt(draggedId.replace('todo-', ''));
                const targetTodoId = parseInt(this.id.replace('todo-', ''));

                // Call the .NET method through Blazor's JS interop
                DotNet.invokeMethodAsync('ToDo', 'HandleDropJS', draggedTodoId, target