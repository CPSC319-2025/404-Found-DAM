import { useState, useRef, useEffect } from "react";

interface Field {
  name: string;
  label: string;
  type: string;
  placeholder?: string;
  value?: string | string[];
  isMulti?: boolean;
  required?: boolean;
}

interface GenericFormProps {
  fields: Field[];
  onSubmit: (formData: Record<string, string | string[]>) => void;
  onCancel: () => void;
  submitButtonText: string;
}

export default function GenericForm({
  fields,
  onSubmit,
  onCancel,
  submitButtonText,
}: GenericFormProps) {
  const formRef = useRef<HTMLDivElement>(null);

  const initialState = fields.reduce((acc, field) => {
    acc[field.name] = field.value || (field.isMulti ? [] : "");
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
        if (field.isMulti) {
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
    console.log("submitting!")
    e.preventDefault();

    const errors = validateForm();
    console.log("Errors", errors);
    setFormErrors(errors);

    if (Object.keys(errors).length === 0) {
      onSubmit(formData);
    }
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
        <h2 className="text-xl font-bold mb-4">Create New</h2>

        <form onSubmit={handleSubmit}>
          {fields.map((field) => (
            <div key={field.name} className="mb-4">
              <label className="block mb-2" htmlFor={field.name}>
                {field.label}
              </label>
              {field.isMulti ? (
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
                        className="bg-blue-500 text-white px-2 py-1 rounded-full mr-2 mt-1 flex items-center"
                      >
                        {entry}
                        <button
                          type="button"
                          className="ml-2 text-white bg-red-500 rounded-full w-5 h-5 flex items-center justify-center"
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
