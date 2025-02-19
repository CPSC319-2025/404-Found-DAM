"use client";

import { useState } from "react";
import GenericForm from "@/app/components/GenericForm";

type ProjectPageProps = {
	params: { slug: string };
};

const editProjectFormFields = [
	{ name: "location", label: "Project Location", type: "text", placeholder: "Enter project location", required: true, 
		value: "Vancouver, BC" },
	{ name: "tags", label: "Tags", type: "text", isMulti: true, placeholder: "Add tags (Press Enter to add one)",
		value: ["tag1", "tag2"]
	},
	{
		name: "admins", label: "Admins", type: "select", isMultiSelect: true, required: true, 
		options: [{ id: "0", name: "dave" }, { id: "1", name: "nehemiah" }, { id: "2", name: "susan" }],
		value: ["2"]
	},
	{
		name: "users", label: "Users", type: "select", isMultiSelect: true, 
		options: [{ id: "0", name: "alice" }, { id: "1", name: "bob" }, { id: "2", name: "charlie" }],
		value: ["0", "1"]
	},
	{
    name: "metadata",
    label: "Custom Metadata",
    type: "custom",
    isCustomMetadata: true,
    value: [
			{ id: "0", name: "Photo Taker", type: "string", enabled: true },
			{ id: "1", name: "Obsolete", type: "boolean", enabled: false },
			{ id: "2", name: "Day of week", type: "number", enabled: true }
		],
		// TODO: handle duplicate names (maybe just handle on backend)
  },
];

interface MetadataField {
	id: number;
	name: string;
	type: string;
}

const isNewMetadataField = id => typeof id === 'string' && id.startsWith("new_");

export default function ProjectPage({ params }: ProjectPageProps) {
	const handleEditProject = (formData) => { 
		const newMetadataFields = formData.metadata?.filter(item => isNewMetadataField(item.id)) ?? [];
		const existingMetadataFields = formData.metadata?.filter(item => !isNewMetadataField(item.id)) ?? [];

		// TODO: when submitting, we need to separate in query
		console.log({ newMetadataFields, existingMetadataFields })
		window.location.reload();
	};

	const onCancel = () => {
		window.location.reload();
	}

	return (
		<div className="sm:p-6 min-h-screen">
      <h1 className="text-2xl fond-bold mb-4">{"Edit Project: " + params.slug}</h1>
			<GenericForm
				isModal={false}
				fields={editProjectFormFields}
				onSubmit={handleEditProject}
        onCancel={onCancel}
				submitButtonText="Save"
			/>
		</div>
	);
}
