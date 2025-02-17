import { useState, useRef, useEffect } from "react";
import Multiselect from "multiselect-react-dropdown";

interface Field {
  name: string;
  label: string;
  type: string;
  placeholder?: string;
  value?: string | string[];
  isMulti?: boolean;
  isMultiSelect?: boolean;
  required?: boolean;
  options?: { name: string; id: string | number }[];
}

interface GenericFormProps {
  title: string;
  fields: Field[];
  onSubmit: (formData: Record<string, string | string[]>) => void;
  onCancel: () => void;
  submitButtonText: string;
}

export default function GenericForm({
  title,
  fields,
  onSubmit,
  onCancel,
  submitButtonText,
}: GenericFormProps) {
  const formRef = useRef<HTMLDivElement>(null);

  const initialState = fields.reduce((acc, field) => {
    acc[field.name] = field.value || (field.isMulti || field.isMultiSelect ? [] : "");
    return acc;
  }, {} as Record<string, string | string[]>);

  const [formData, setFormData] = useState(initialState);
  const [formErrors, setFormErrors] = useState<Record<string, string>>({});

  const handleChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const { name, value } = e.target;

    setFormErrors((prevErrors) => {
      const updatedErrors = { ...prevErrors };
      if (updatedErrors[name]) {
        delete updatedErrors[name];
      }
      return updatedErrors;
    });

    setFormData((prevData) => ({ ...prevData, [name]: value }));
  };

  const handleMultiValueChange = (e: React.KeyboardEvent<HTMLInputElement>, name: string) => {
    if (e.key === "Enter") {
      e.preventDefault();
      const value = (e.target as HTMLInputElement).value.trim();

      if (value) {
        setFormData((prevData) => {
          const existingValues = prevData[name] as string[] || [];

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
    }
  };

  const removeEntry = (name: string, entry: string) => {
    setFormData((prevData) => ({
      ...prevData,
      [name]: (prevData[name] as string[]).filter((e) => e !== entry),
    }));
  };

  const validateForm = () => {
    const errors: Record<string, string> = {};

    fields.forEach((field) => {
      const fieldValue = formData[field.name];

      if (field.required) {
        if (field.isMulti || field.isMultiSelect) {
          if (!Array.isArray(fieldValue) || (fieldValue as string[]).length === 0) {
            errors[field.name] = `${field.label} is required`;
          }
        } else {
          if (!fieldValue) {
            errors[field.name] = `${field.label} is required`;
          }
        }
      }
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
  };

  const handleRemove = (selectedList: any[], name: string) => {
    setFormData((prevData) => ({
      ...prevData,
      [name]: selectedList.map((item) => item.id),
    }));
  };

  useEffect(() => {
    function handleClickOutside(event: MouseEvent) {
      if (formRef.current && !formRef.current.contains(event.target as Node)) {
        onCancel();
      }
    }

    document.addEventListener("mousedown", handleClickOutside);
    return () => {
      document.removeEventListener("mousedown", handleClickOutside);
    };
  }, [onCancel]);

  return (
    <div className="fixed inset-0 bg-gray-500 bg-opacity-50 flex justify-center items-center">
      <div ref={formRef} className="bg-white p-6 rounded shadow-lg w-96">
        <h2 className="text-xl font-bold mb-4">{title}</h2>

        <form onSubmit={handleSubmit}>
          {fields.map((field) => (
            <div key={field.name} className="mb-4">
              <label className="block mb-2" htmlFor={field.name}>
                {`${field.label} ${field.required ? ' *' : ''}`}
              </label>
              {field.isMultiSelect ? (
                <Multiselect
                  options={field.options || []}
                  onSelect={(selectedList) => handleSelect(selectedList, field.name)}
                  onRemove={(selectedList) => handleRemove(selectedList, field.name)}
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
                      <span
                        key={index}
                        className="chip"
                      >
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
              ) : (
                <input
                  id={field.name}
                  name={field.name}
                  type={field.type}
                  placeholder={field.placeholder}
                  value={formData[field.name] as string}
                  onChange={handleChange}
                  className="w-full p-2 border rounded"
                />
              )}
              {formErrors[field.name] && (
                <p className="text-red-500 text-sm mt-1">{formErrors[field.name]}</p>
              )}
            </div>
          ))}

          <div><i className="opacity-50">* Required field</i></div>

          <div className="flex justify-end space-x-2">
            <button
              type="button"
              onClick={onCancel}
              className="bg-gray-300 text-black p-2 rounded"
            >
              Cancel
            </button>
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
