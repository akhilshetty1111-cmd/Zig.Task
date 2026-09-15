export interface Attachment {
  id: string;
  taskId: string;
  uploadedBy: string;
  uploadedByName: string;
  fileName: string;
  fileUrl: string;
  fileSizeBytes: number;
  contentType: string;
  createdAt: string;
}
