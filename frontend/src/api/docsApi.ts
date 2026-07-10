import { httpClient } from "./httpClient";
import type { ApiResponse } from "../types/auth";

export type DocumentationSummary = {
  slug: string;
  title: string;
  description: string;
  category: string;
  roles: string[];
  updatedAt: string;
};

export type DocumentationDetail = DocumentationSummary & {
  contentMarkdown: string;
};

export async function getDocumentationList() {
  const response = await httpClient.get<ApiResponse<DocumentationSummary[]>>("/api/docs");
  return response.data.data ?? [];
}

export async function getDocumentationDetail(slug: string) {
  const response = await httpClient.get<ApiResponse<DocumentationDetail>>(`/api/docs/${slug}`);
  return response.data.data;
}

export async function updateDocumentation(slug: string, contentMarkdown: string) {
  const response = await httpClient.put<ApiResponse<DocumentationDetail>>(`/api/docs/${slug}`, { contentMarkdown });
  return response.data.data;
}

export async function downloadDocumentationPdf(slug: string) {
  const response = await httpClient.get<Blob>(`/api/docs/${slug}/pdf`, { responseType: "blob" });
  return response.data;
}
