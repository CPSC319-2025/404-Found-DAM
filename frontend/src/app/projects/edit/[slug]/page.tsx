"use client";

import { useEffect, useState } from "react";
import GenericForm, {
  FormData,
  CustomMetadataField,
  Field as FormFieldType,
  ChangeType,
} from "@/app/components/GenericForm";
import { fetchWithAuth } from "@/app/utils/api/api";
import { toast } from "react-toastify";
import { User, Project, Tag, ProjectMetadataField } from "@/app/types";
import { useRouter } from "next/navigation";
import PopupModal from "@/app/components/ConfirmModal";
import { useUser } from "@/app/context/UserContext";
import metadata from "next/dist/server/typescript/rules/metadata";

interface ProjectWithMetadata extends Project {
  tags: Tag[];
  metadataFields: ProjectMetadataField[];
}

type ProjectPageProps = {
  params: { slug: string };
};

// const isNewMetadataField = (id: string) => id.startsWith("new_");

const editProjectFormFields: FormFieldType[] = [
  {
    name: "location",
    label: "Project Location",
    type: "text",
    placeholder: "Enter project location",
    required: true,
  },
  {
    name: "admins",
    label: "Admins",
    type: "select",
    isMultiSelect: true,
  },
  {
    name: "users",
    label: "Users",
    type: "select",
    isMultiSelect: true,
  },
  {
    name: "tags",
    label: "Tags",
    type: "select",
    isMultiSelect: true,
  },
  {
    name: "metadata",
    label: "Custom Metadata",
    type: "custom",
    isCustomMetadata: true,
  },
];

export default function ProjectPage({ params }: ProjectPageProps) {
  const router = useRouter();
  const { user } = useUser();

  const [confirmPopup, setConfirmPopup] = useState<boolean>(false);
  const [confirmMessages, setConfirmMessages] = useState<string[]>([]);

  const [loading, setLoading] = useState(true);

  const [formDisabled, setFormDisabled] = useState(true);

  const [tagOptions, setTagOptions] = useState<string[]>([]);

  const [formFields, setFormFields] = useState<FormFieldType[]>(
    editProjectFormFields
  );

  const [allUsers, setAllUsers] = useState<User[]>([]);

  const [regularUserOptions, setRegularUserOptions] = useState<User[]>([]);
  const [adminOptions, setAdminOptions] = useState<User[]>([]);

  const [projectName, setProjectName] = useState<string>("");

  const [formData, setFormData] = useState<FormData>({});

  const fetchTags = async (): Promise<string[]> => {
    try {
      const response = await fetchWithAuth("/tags");
      if (!response.ok) throw new Error("Failed to fetch tags");

      const tags: string[] = await response.json();
      return tags;
    } catch (error) {
      console.error("Error fetching tags:", error);
      return [];
    }
  };

  const onUserChange = (
    changeItem: { id: number; name: string },
    fieldName: string,
    changeType: ChangeType
  ) => {
    if (fieldName === "admins") {
      if (changeType === "select") {
        setRegularUserOptions((prev) =>
          prev.filter((user) => user.userID !== changeItem.id)
        );
      } else {
        const userToAddBack = allUsers.find(
          (user) => user.userID === changeItem.id
        );
        setRegularUserOptions((prev) => [...prev, userToAddBack!]);
      }
    } else {
      if (changeType === "select") {
        setAdminOptions((prev) =>
          prev.filter((admin) => admin.userID !== changeItem.id)
        );
      } else {
        const userToAddBack = allUsers.find(
          (user) => user.userID === changeItem.id
        );
        setAdminOptions((prev) => [...prev, userToAddBack!]);
      }
    }
  };

  const onSubmit = (updatedFormData: FormData) => {
    let anyDeletions = false;

    const metadataFields = formFields.find(
      (field) => field.name === "metadata"
    );

    if (updatedFormData?.metadata && metadataFields) {
      const oldMetadata = metadataFields.value as CustomMetadataField[];
      const newMetadata = updatedFormData.metadata as CustomMetadataField[];

      anyDeletions = oldMetadata.some(
        (oldField) =>
          !newMetadata.some((newField) => newField.id === oldField.id)
      );
    }

    const confirmMessages = [];

    if (anyDeletions) {
      confirmMessages.push(
        "Warning: any data related to deleted metadata will be lost."
      );
    }

    confirmMessages.push(
      " Any disabled custom metadata fields will become unusable."
    );

    setConfirmMessages(confirmMessages);
    setConfirmPopup(true);
    setFormData(updatedFormData);
  };

  const onConfirmSubmit = () => {
    handleEditProject(formData);
  };

  const handleEditProject = async (updatedFormData: FormData) => {
    setConfirmPopup(false);

    const admins = (updatedFormData.admins as number[]).map((id) => {
      return {
        userID: id,
        role: "Admin",
      };
    });

    const regularUsers = (updatedFormData.users as number[]).map((id) => {
      return {
        userID: id,
        role: "Regular",
      };
    });

    const memberships = admins.concat(regularUsers);

    const customMetadata = (
      updatedFormData.metadata as CustomMetadataField[]
    ).map((field) => {
      return {
        fieldName: field.name,
        fieldType: field.type,
        isEnabled: field.enabled,
      };
    });

    const tags = (updatedFormData.tags as string[]).map((tag) => {
      return {
        name: tag,
      };
    });

    const updatedProject = {
      location: updatedFormData.location,
      memberships,
      tags,
      customMetadata,
    };

    setFormDisabled(true);

    const response = await fetchWithAuth(`projects/${params.slug}`, {
      method: "PATCH",
      body: JSON.stringify(updatedProject),
    });

    if (!response.ok) {
      console.error(
        `Failed to update project (Status: ${response.status} - ${response.statusText}`
      );
      setFormDisabled(false);
      setLoading(false);

      const parsedResponse: any = await response.json();
      toast.error(parsedResponse.detail);

      return;
    }

    toast.success("Successfully updated the project's metadata");

    router.push("/projects");
  };

  const fetchUsers = async () => {
    try {
      const response = await fetchWithAuth("users");
      if (!response.ok) {
        throw new Error(
          `Failed to fetch users (Status: ${response.status} - ${response.statusText})`
        );
      }
      return (await response.json()).users as User[];
    } catch (error) {
      console.error("[Diagnostics] Error fetching users: ", error);
      return [] as User[];
    }
  };

  const fetchProject = async () => {
    const response = await fetchWithAuth(`projects/${params.slug}`);

    if (!response.ok) {
      throw new Error("Failed to get project.");
    }

    const project = await response.json();

    if (!project) {
      throw new Error("No project returned from the API.");
    }

    return project as ProjectWithMetadata;
  };

  const onLoad = () => {
    fetchUsers()
      .then((users) => {
        setAllUsers(users);
        setAdminOptions(users);
        setRegularUserOptions(users);
        // Now fetch tags
        return fetchTags();
      })
      .then((tags) => {
        // Set state for tags and pass them along in the chain
        setTagOptions(tags);
        return fetchProject().then((project) => ({ project, tags }));
      })
      .then(({ project, tags }) => {
        const updatedFormFields = [...editProjectFormFields];

        setProjectName(project.name!);

        updatedFormFields.forEach((field) => {
          if (field.name === "location") {
            field.value = project.location;
          }

          if (field.name === "tags") {
            field.value = project.tags.map((tag) => tag.name);
            field.options = tags.map((tagName) => ({
              id: tagName,
              name: tagName,
            }));
          }

          if (field.name === "admins") {
            field.value = project.admins.map((admin) => admin.userID);
          }

          if (field.name === "users") {
            field.value = project.regularUsers.map((user) => user.userID);
          }

          if (field.name === "metadata") {
            field.value = project.metadataFields.map((metadata) => {
              return {
                id: String(metadata.fieldID),
                name: metadata.fieldName,
                type: metadata.fieldType,
                enabled: metadata.isEnabled,
              } as CustomMetadataField;
            });
          }
        });

        setFormFields(updatedFormFields);

        const selectedAdmins = project.admins.map((admin) => admin.userID);
        const selectedUsers = project.regularUsers.map((user) => user.userID);

        setAdminOptions((prev) =>
          prev.filter((admin) => !selectedUsers.includes(admin.userID))
        );

        setRegularUserOptions((prev) =>
          prev.filter((user) => !selectedAdmins.includes(user.userID))
        );

        if (
          user?.superadmin ||
          project.admins.find((admin) => admin.userID === user!.userID)
        ) {
          setFormDisabled(false);
        }

        setLoading(false);
      })
      .catch((error) => {
        console.error("Error in onLoad:", error);
        setLoading(false);
      });
  };

  useEffect(() => {
    onLoad();
  }, []);

  // whenever a user selects an admin/regular user we need to update the form (filter options)
  useEffect(() => {
    const updatedFormFields = [...editProjectFormFields];

    updatedFormFields.forEach((field) => {
      if (field.name === "admins") {
        field.options = adminOptions.map((user) => ({
          id: user.userID,
          name: `${user.name} (${user.email})`,
        }));
        field.onChange = onUserChange;
      }

      if (field.name === "users") {
        field.options = regularUserOptions.map((user) => ({
          id: user.userID,
          name: `${user.name} (${user.email})`,
        }));
        field.onChange = onUserChange;
      }
    });

    setFormFields(updatedFormFields);
  }, [adminOptions, regularUserOptions]);

  const onCancel = () => {
    router.push("/projects");
  };

  return (
    <div className="sm:p-6 min-h-screen">
      <h1 className="text-2xl font-bold mb-4">
        {"Edit Project: " + projectName}
      </h1>

      {loading && (
        <div className="flex justify-center items-center mb-4">
          <div className="w-6 h-6 border-4 border-blue-500 border-t-transparent rounded-full animate-spin"></div>
          <span className="ml-2 text-blue-500">Loading...</span>
        </div>
      )}

      {!loading && (
        <GenericForm
          title=""
          isModal={false}
          fields={formFields}
          onSubmit={onSubmit}
          onCancel={onCancel}
          submitButtonText="Save"
          disabled={formDisabled}
        />
      )}

      <PopupModal
        title="Are you sure you want to update this project?"
        isOpen={confirmPopup}
        onClose={() => setConfirmPopup(false)}
        onConfirm={onConfirmSubmit}
        messages={confirmMessages}
      />
    </div>
  );
}
