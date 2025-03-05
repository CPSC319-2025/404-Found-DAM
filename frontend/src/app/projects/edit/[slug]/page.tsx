"use client";

import { useState } from "react";
import GenericForm from "@/app/components/GenericForm";

type ProjectPageProps = {
  params: { slug: string };
};

interface MetadataField {
  id: string | number;
  name: string;
  type: string;
  enabled?: boolean;
}

const isNewMetadataField = (id: string | number) =>
  typeof id === "string" && id.startsWith("new_");

export default function ProjectPage({ params }: ProjectPageProps) {
  const [formData, setFormData] = useState({
    location: "Vancouver, BC",
    admins: ["2"],
    users: ["0", "1"],
    tags: ["tag1", "tag2"],
    metadata: [
      { id: "0", name: "Photo Taker", type: "string", enabled: true },
      { id: "1", name: "Obsolete", type: "boolean", enabled: false },
      { id: "2", name: "Day of week", type: "number", enabled: true },
    ],
  });

  const [loading, setLoading] = useState(false);

  const editProjectFormFields = [
    {
      name: "location",
      label: "Project Location",
      type: "text",
      placeholder: "Enter project location",
      required: true,
      value: formData.location,
    },
    {
      name: "admins",
      label: "Admins",
      type: "select",
      isMultiSelect: true,
      required: true,
      options: [
        { id: "0", name: "dave" },
        { id: "1", name: "nehemiah" },
        { id: "2", name: "susan" },
      ],
      value: formData.admins,
    },
    {
      name: "users",
      label: "Users",
      type: "select",
      isMultiSelect: true,
      options: [
        { id: "0", name: "alice" },
        { id: "1", name: "bob" },
        { id: "2", name: "charlie" },
      ],
      value: formData.users,
    },
    {
      name: "tags",
      label: "Tags",
      type: "text",
      isMulti: true,
      placeholder: "Add tags (Press Enter to add one)",
      value: formData.tags,
    },
    {
      name: "metadata",
      label: "Custom Metadata",
      type: "custom",
      isCustomMetadata: true,
      value: formData.metadata,
    },
  ];

  const handleEditProject = async (updatedFormData) => {
    setLoading(true);

    await new Promise((resolve) => setTimeout(resolve, 1000));

    const newMetadataFields =
      updatedFormData.metadata?.filter((item) => isNewMetadataField(item.id)) ??
      [];
    const existingMetadataFields =
      updatedFormData.metadata?.filter(
        (item) => !isNewMetadataField(item.id)
      ) ?? [];

    console.log({ newMetadataFields, existingMetadataFields });

    setFormData(updatedFormData);
    setLoading(false);
  };

  const onCancel = () => {
    window.location.reload();
  };

  return (
    <div className="sm:p-6 min-h-screen">
      <h1 className="text-2xl font-bold mb-4">
        {"Edit Project: " + params.slug}
      </h1>

      {loading && (
        <div className="flex justify-center items-center mb-4">
          <div className="w-6 h-6 border-4 border-blue-500 border-t-transparent rounded-full animate-spin"></div>
          <span className="ml-2 text-blue-500">Saving...</span>
        </div>
      )}

      {!loading && (
      <GenericForm
        isModal={false}
        fields={editProjectFormFields}
        onSubmit={handleEditProject}
        onCancel={onCancel}
        submitButtonText="Save"
        disabled={loading}
      />
      )}
    </div>
  );
}
