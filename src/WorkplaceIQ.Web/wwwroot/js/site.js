// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

document.addEventListener("click", event => {
  const trigger = event.target.closest("[data-iq-action]");
  if (!trigger) {
    return;
  }

  const modalElement = document.getElementById("iqItemActionModal");
  const form = document.getElementById("iqItemActionForm");
  if (!modalElement || !form || !window.bootstrap) {
    return;
  }

  const action = trigger.dataset.iqAction;
  const config = getItemActionConfig(action);
  if (!config) {
    return;
  }

  document.getElementById("iqItemActionModalLabel").textContent = config.title;
  document.getElementById("iqItemActionSubmit").textContent = config.submit;
  document.getElementById("iqItemActionSubmit").className = config.submitClass;
  document.querySelector("label[for=\"iqItemActionBody\"]").textContent = config.bodyLabel || "Comment";
  document.getElementById("iqItemActionType").value = trigger.dataset.iqItemType || "";
  document.getElementById("iqItemActionId").value = trigger.dataset.iqItemId || "";
  document.getElementById("iqItemActionReturnUrl").value = window.location.href;
  document.getElementById("iqItemActionMessage").textContent = config.message || "";

  form.action = config.action;

  setFieldVisible("title", config.fields.includes("title"));
  setFieldVisible("body", config.fields.includes("body"));
  setFieldVisible("label", config.fields.includes("label"));

  const title = document.getElementById("iqItemActionTitle");
  const body = document.getElementById("iqItemActionBody");
  const label = document.getElementById("iqItemActionLabel");

  title.value = config.fields.includes("title") ? (trigger.dataset.iqItemTitle || "") : "";
  body.value = config.prefillBody ? (trigger.dataset.iqItemBody || "") : "";
  label.value = "";

  title.required = config.fields.includes("title");
  body.required = config.requiredBody;
  label.required = config.fields.includes("label");

  bootstrap.Modal.getOrCreateInstance(modalElement).show();
});

function setFieldVisible(field, visible) {
  const element = document.querySelector(`[data-iq-field="${field}"]`);
  if (element) {
    element.hidden = !visible;
  }
}

function getItemActionConfig(action) {
  switch (action) {
    case "comment":
      return {
        title: "Add comment",
        submit: "Add comment",
        submitClass: "btn btn-primary",
        action: "/Content/AddComment",
        fields: ["body"],
        requiredBody: true
      };
    case "label":
      return {
        title: "Add label",
        submit: "Add label",
        submitClass: "btn btn-primary",
        action: "/Content/AddLabel",
        fields: ["label"],
        requiredBody: false
      };
    case "edit":
      return {
        title: "Edit item",
        submit: "Save changes",
        submitClass: "btn btn-primary",
        action: "/Content/Edit",
        fields: ["title", "body"],
        bodyLabel: "Body",
        prefillBody: true,
        requiredBody: false
      };
    case "delete":
      return {
        title: "Delete item",
        submit: "Delete",
        submitClass: "btn btn-danger",
        action: "/Content/Delete",
        fields: [],
        requiredBody: false,
        message: "This item will be removed from the current view."
      };
    default:
      return null;
  }
}
