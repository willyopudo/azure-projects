import http from "../http-common";

class UploadFilesService {
  upload(file, tags, onUploadProgress) {
    let formData = new FormData();

    formData.append("file", file);
    formData.append("tags", JSON.stringify(tags)); // Add tags to the payload

    return http.post("/Document/upload", formData, {
      headers: {
        "Content-Type": "multipart/form-data",
      },
      onUploadProgress,
    });
  }

  getFiles() {
    return http.get("/Document/files");
  }

  // New method to call the download API
  downloadFile(fileId) {
    return http.get(`/Document/download/${fileId}`);
  }

  // New method to call the delete API
  deleteFile(fileId) {
    return http.delete(`/Document/delete/${fileId}`);
  }
}

export default new UploadFilesService();