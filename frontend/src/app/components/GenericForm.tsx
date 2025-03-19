import React, { useState, useRef, useEffect } from "react";
import Multiselect from "multiselect-react-dropdown";

type AvailableCustomFieldTypes = "number" | "string" | "boolean";

export interface CustomMetadataField {
  id: string;
  name: string;
  type: AvailableCustomFieldTypes;
  enabled: boolean;
}

type FieldValue = string | number | boolean | string[] | CustomMetadataField[];

interface Field {
  name: string;
  label: string;
  type: string;
  placeholder?: string;
  value?: FieldValue;
  isMulti?: boolean;
  isMultiSelect?: boolean;
  isCustomMetadata?: boolean; // hardcoding this type for now. Cant really think of reason to make more generic
  required?: boolean;
  options?: { name: string; id: string }[]; // TODO: this may need to change depending on database!
}

export type FormData = Record<string, FieldValue>;

interface GenericFormProps {
  title: string;
  fields: Field[];
  onSubmit: (formData: FormData) => void;
  onCancel: () => void;
  submitButtonText: string;
  isModal?: boolean;
}

export default function GenericForm({
  title,
  fields,
  onSubmit,
  onCancel,
  submitButtonText,
  isModal = true,
}: GenericFormProps) {
  const formRef = useRef<HTMLDivElement>(null);

  const initialState = fields.reduce((acc, field) => {
    if (field.isMulti || field.isMultiSelect) {
      acc[field.name] = Array.isArray(field.value) ? field.value : [];
    } else {
      acc[field.name] = field.value || "";
    }
    return acc;
  }, {} as FormData);

  const [formData, setFormData] = useState(initialState);
  const [formErrors, setFormErrors] = useState<Record<string, string>>({});
  const [hasEdited, setHasEdited] = useState(false);

  const handleChange = (e: any) => {
    const { name, value } = e.target;

    setFormErrors((prevErrors) => {
      const updatedErrors = { ...prevErrors };
      if (updatedErrors[name]) {
        delete updatedErrors[name];
      }
      return updatedErrors;
    });

    setFormData((prevData) => {
      let newValue: string | number | boolean = value;

      const field = fields.find((f) => f.name === name);

      if (field) {
        if (field.type === "number") {
          newValue = Number(value);
        } else if (field.type === "boolean") {
          newValue = Boolean(value);
        }
      }

      return { ...prevData, [name]: newValue };
    });

    if (!hasEdited) {
      setHasEdited(() => true);
    }
  };

  const handleMultiValueChange = (
    e: React.KeyboardEvent<HTMLInputElement>,
    name: string
  ) => {
    if (e.key === "Enter") {
      e.preventDefault();
      const value = (e.target as HTMLInputElement).value.trim();

      if (value) {
        setFormData((prevData) => {
          const existingValues = (prevData[name] as string[]) || [];

          if (!existingValues.includes(value)) {
            return {
              ...prevData,
              [name]: [...existingValues, value],
            };
          }

          return prevData;
        });

        (e.target as HTMLInputElement).value = "";

        setFormErrors((prevErrors) => {
          const updatedErrors = { ...prevErrors };
          if (updatedErrors[name]) {
            delete updatedErrors[name];
          }
          return updatedErrors;
        });
      }

      if (!hasEdited) {
        setHasEdited(() => true);
      }
    }
  };

  const removeEntry = (name: string, entry: string) => {
    setFormData((prevData) => ({
      ...prevData,
      [name]: (prevData[name] as string[]).filter((e) => e !== entry),
    }));

    if (!hasEdited) {
      setHasEdited(() => true);
    }
  };

  const handleComplexChange = (
    index: number,
    fieldName: string,
    key: keyof CustomMetadataField,
    value: string | boolean
  ) => {
    setFormData((prevData) => {
      const updatedArray = [...(prevData[fieldName] as CustomMetadataField[])];
      updatedArray[index] = { ...updatedArray[index], [key]: value };
      return { ...prevData, [fieldName]: updatedArray };
    });

    if (!hasEdited) {
      setHasEdited(() => true);
    }
  };

  const addComplexEntry = (fieldName: string) => {
    setFormData((prevData) => ({
      ...prevData,
      [fieldName]: [
        ...(prevData[fieldName] as CustomMetadataField[]),
        { id: "new_" + Date.now(), name: "", type: "string", enabled: true },
      ],
    }));

    if (!hasEdited) {
      setHasEdited(() => true);
    }
  };

  const removeComplexEntry = (fieldName: string, index: number) => {
    setFormData((prevData) => ({
      ...prevData,
      [fieldName]: (prevData[fieldName] as CustomMetadataField[]).filter(
        (_, i) => i !== index
      ),
    }));

    if (!hasEdited) {
      setHasEdited(() => true);
    }
  };

  const validateForm = () => {
    const errors: Record<string, string> = {};

    fields.forEach((field) => {
      const fieldValue = formData[field.name];

      if (field.required) {
        if (field.isMulti || field.isMultiSelect) {
          if (
            !Array.isArray(fieldValue) ||
            (fieldValue as string[]).length === 0
          ) {
            errors[field.name] = `${field.label} is required`;
          }
        } else {
          if (!fieldValue) {
            errors[field.name] = `${field.label} is required`;
          }
        }
      }

      // TODO: error for empty metadata field name
    });

    return errors;
  };

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();

    const errors = validateForm();
    setFormErrors(errors);

    if (Object.keys(errors).length === 0) {
      onSubmit(formData);
    }
  };

  const handleSelect = (selectedList: any[], name: string) => {
    setFormData((prevData) => ({
      ...prevData,
      [name]: selectedList.map((item) => item.id),
    }));

    setFormErrors((prevErrors) => {
      const updatedErrors = { ...prevErrors };
      if (updatedErrors[name] && selectedList.length > 0) {
        delete updatedErrors[name];
      }
      return updatedErrors;
    });

    if (!hasEdited) {
      setHasEdited(() => true);
    }
  };

  const handleRemove = (selectedList: any[], name: string) => {
    setFormData((prevData) => ({
      ...prevData,
      [name]: selectedList.map((item) => item.id),
    }));

    if (!hasEdited) {
      setHasEdited(() => true);
    }
  };

  useEffect(() => {
    if (isModal) {
      const handleClickOutside = (event: MouseEvent) => {
        if (
          formRef.current &&
          !formRef.current.contains(event.target as Node)
        ) {
          onCancel();
        }
      };

      document.addEventListener("mousedown", handleClickOutside);
      return () => {
        document.removeEventListener("mousedown", handleClickOutside);
      };
    }
  }, [onCancel, isModal]);

  return (
    <div
      className={
        isModal
          ? "fixed inset-0 bg-gray-500 bg-opacity-50 flex justify-center items-center"
          : "container mr-auto p-0"
      }
    >
      <div
        ref={formRef}
        className={
          isModal
            ? "bg-white p-6 rounded shadow-lg w-96 max-h-screen overflow-y-auto"
            : "bg-white p-6 shadow-lg max-w-xl"
        }
      >
        <h2 className="text-xl font-bold mb-4">{title}</h2>

        <form onSubmit={handleSubmit}>
          {fields.map((field) => (
            <div key={field.name} className="mb-4">
              <label className="block mb-2" htmlFor={field.name}>
                {`${field.label} ${field.required ? " *" : ""}`}
              </label>
              {field.isMultiSelect ? (
                <Multiselect
                  options={field.options || []}
                  selectedValues={field.options?.filter((option) =>
                    (formData[field.name] as string[]).includes(option.id)
                  )}
                  onSelect={(selectedList) =>
                    handleSelect(selectedList, field.name)
                  }
                  onRemove={(selectedList) =>
                    handleRemove(selectedList, field.name)
                  }
                  displayValue="name"
                  className="custom-multiselect w-full p-2 border rounded"
                  closeIcon="cancel"
                  avoidHighlightFirstOption
                />
              ) : field.isMulti ? (
                <div>
                  <input
                    id={field.name}
                    name={field.name}
                    type="text"
                    placeholder={field.placeholder}
                    onKeyDown={(e) => handleMultiValueChange(e, field.name)}
                    className="w-full p-2 border rounded"
                  />
                  <div className="flex flex-wrap mt-2">
                    {(formData[field.name] as string[]).map((entry, index) => (
                      <span key={index} className="chip">
                        {entry}
                        <button
                          type="button"
                          className="ml-2 text-2xl text-white bg-transparent border-none cursor-pointer"
                          onClick={() => removeEntry(field.name, entry)}
                        >
                          ×
                        </button>
                      </span>
                    ))}
                  </div>
                </div>
              ) : field.isCustomMetadata ? (
                <div>
                  {((formData[field.name] as CustomMetadataField[]) || []).map(
                    (entry, index) => (
                      <div
                        key={entry.id}
                        className="flex mb-2 items-center border p-2 rounded-md w-full"
                      >
                        <input
                          type="text"
                          placeholder="Metadata Name"
                          value={entry.name}
                          onChange={(e) =>
                            handleComplexChange(
                              index,
                              field.name,
                              "name",
                              e.target.value
                            )
                          }
                          className="p-2 border rounded w-full sm:max-w-3xs"
                        />

                        <select
                          value={entry.type}
                          onChange={(e) =>
                            handleComplexChange(
                              index,
                              field.name,
                              "type",
                              e.target.value
                            )
                          }
                          className="p-2 border rounded ml-2 w-full sm:max-w-3xs"
                        >
                          <option value="text">Text</option>
                          <option value="number">Number</option>
                          <option value="boolean">Yes / No</option>
                        </select>

                        <div className="flex items-center ml-2 sm:ml-2 space-x-3">
                          <label className="flex items-center cursor-pointer">
                            <input
                              type="checkbox"
                              checked={entry.enabled}
                              onChange={(e) =>
                                handleComplexChange(
                                  index,
                                  field.name,
                                  "enabled",
                                  e.target.checked
                                )
                              }
                              className="hidden"
                            />
                            <span
                              className={`w-10 h-5 flex items-center rounded-full p-1 transition duration-300 ${
                                entry.enabled ? "bg-green-500" : "bg-gray-300"
                              }`}
                            >
                              <span
                                className={`bg-white w-4 h-4 rounded-full shadow-md transform transition duration-300 ${
                                  entry.enabled ? "translate-x-5" : ""
                                }`}
                              ></span>
                            </span>
                          </label>

                          <button
                            type="button"
                            onClick={() =>
                              removeComplexEntry(field.name, index)
                            }
                            className="text-red-500"
                          >
                            ✕
                          </button>
                        </div>
                      </div>
                    )
                  )}
                  <button
                    type="button"
                    onClick={() => addComplexEntry(field.name)}
                    className="text-blue-500"
                  >
                    + Add Entry
                  </button>
                </div>
              ) : field.type === "number" ? (
                <input
                  id={field.name}
                  name={field.name}
                  type="number"
                  placeholder={field.placeholder}
                  value={formData[field.name] as string}
                  onChange={handleChange}
                  className="w-full p-2 border rounded"
                />
              ) : field.type === "boolean" ? (
                <select
                  id={field.name}
                  name={field.name}
                  value={String(formData[field.name])}
                  onChange={handleChange}
                  className="w-full p-2 border rounded"
                >
                  <option value="">Select...</option>
                  <option value={"true"}>Yes</option>
                  <option value={"false"}>No</option>
                </select>
              ) : (
                <input
                  id={field.name}
                  name={field.name}
                  type="text"
                  placeholder={field.placeholder}
                  value={formData[field.name] as string}
                  onChange={handleChange}
                  className="w-full p-2 border rounded"
                />
              )}
              {formErrors[field.name] && (
                <p className="text-red-500 text-sm mt-1">
                  {formErrors[field.name]}
                </p>
              )}
            </div>
          ))}

          <div>
            <i className="opacity-50">* Required field</i>
          </div>

          <div className="flex justify-end space-x-2">
            {(isModal || hasEdited) && (
              <button
                type="button"
                onClick={onCancel}
                className="bg-gray-300 text-black p-2 rounded"
              >
                Cancel
              </button>
            )}
            <button
              type="submit"
              className="bg-blue-500 text-white p-2 rounded"
            >
              {submitButtonText}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}
