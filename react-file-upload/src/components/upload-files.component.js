import React, { Component } from "react";
import UploadService from "../services/upload-files.service";
import $ from "jquery";
import "select2";
import "select2/dist/css/select2.css";

export default class UploadFiles extends Component {
  constructor(props) {
      super(props);
      this.selectFile = this.selectFile.bind(this);
      this.handleTagChange = this.handleTagChange.bind(this);
      this.upload = this.upload.bind(this);
  
      this.state = {
        selectedFiles: undefined,
        currentFile: undefined,
        progress: 0,
        message: "",
        fileInfos: [],
        tags: [], // State to store selected tags
        availableTags: ["personal", "corporate", "family", "community", "government", "public"], // Available tags
      }; 
  }

  componentDidMount() {

    // Initialize Select2
    $(this.selectRef).select2({
      placeholder: "Select tags",
      allowClear: true,
    });

    // Handle change event for Select2
    $(this.selectRef).on("change", (event) => {
        const selectedTags = $(event.target).val(); // Get selected values
        this.setState({ tags: selectedTags });
    });

    // Fetch files from the backend
    UploadService.getFiles().then((response) => {
      this.setState({
        fileInfos: response.data,
      });
    });
  }

  componentWillUnmount() {
    // Destroy Select2 instance
    $(this.selectRef).select2("destroy");
  }
    
  selectFile(event) {
      this.setState({
        selectedFiles: event.target.files,
      });
  }

  handleTagChange(event) {
      const options = event.target.options;
      const selectedTags = [];
      for (let i = 0; i < options.length; i++) {
          if (options[i].selected) {
              selectedTags.push(options[i].value);
          }
      }
      this.setState({ tags: selectedTags });
  }

  handleFileDownload(fileId) {
    UploadService.downloadFile(fileId)
      .then((response) => {
        // Redirect to the blob URL returned by the API
        window.location.href = response.data.blobUrl;
      })
      .catch((error) => {
        console.error("Error downloading file:", error);
        alert("Failed to download the file. Please try again.");
      });
  }

  handleFileDelete(fileId) {
    UploadService.deleteFile(fileId)
      .then((response) => {
        this.setState((prevState) => ({
          fileInfos: prevState.fileInfos.filter((file) => file.id !== fileId),
          message: response.data.message,
        }));
      })
      .catch((error) => {
        console.error("Error deleting file:", error);
        alert("Failed to delete the file. Please try again.");
      });
  }
    
  upload() {
      let currentFile = this.state.selectedFiles[0];
      const { tags } = this.state;

      this.setState({
        progress: 0,
        currentFile: currentFile,
      });
  
      UploadService.upload(currentFile, tags, (event) => {
        this.setState({
          progress: Math.round((100 * event.loaded) / event.total),
        });
      })
        .then((response) => {
          this.setState({
            message: response.data.message,
          });
          return UploadService.getFiles();
        })
        .then((files) => {
          this.setState({
            fileInfos: files.data,
          });
        })
        .catch(() => {
          this.setState({
            progress: 0,
            message: "Could not upload the file!",
            currentFile: undefined,
          });
        });
  
      this.setState({
        selectedFiles: undefined,
      });
  }

  render() {
      const {
          selectedFiles,
          currentFile,
          progress,
          message,
          fileInfos,
          tags,
          availableTags,
      } = this.state;
    
      return (
          <div>
            {currentFile && (
              <div className="progress">
                <div
                  className="progress-bar progress-bar-info progress-bar-striped"
                  role="progressbar"
                  aria-valuenow={progress}
                  aria-valuemin="0"
                  aria-valuemax="100"
                  style={{ width: progress + "%" }}
                >
                  {progress}%
                </div>
              </div>
            )}
    
            <label className="btn btn-default">
              <input type="file" onChange={this.selectFile} />
            </label>

            {/* Multi-select input for tags */}
            <select
              multiple
              className="form-control"
              ref={(ref) => (this.selectRef = ref)} // Add a ref for Select2 initialization
              style={{ marginTop: "10px", marginBottom: "10px" }}
            >
              {availableTags.map((tag, index) => (
                <option key={index} value={tag}>
                  {tag}
                </option>
              ))}
            </select>
    
            <button
              className="btn btn-success mt-2 mb-2"
              disabled={!selectedFiles}
              onClick={this.upload}
            >
              Upload
            </button>
    
            <div className="alert alert-light" role="alert">
              {message}
            </div>
    
            <div className="card">
              <div className="card-header">List of Files</div>
              <ul className="list-group list-group-flush">
                {fileInfos &&
                  fileInfos.map((file, index) => (
                    <li
                      className="list-group-item d-flex justify-content-between align-items-center"
                      key={index}
                    >
                      <span
                        style={{ cursor: "pointer", color: "blue", textDecoration: "underline" }}
                        onClick={() => this.handleFileDownload(file.id)} // Download handler
                      >
                        {file.fileName}
                      </span>
                      <button
                        className="btn btn-danger btn-sm"
                        onClick={() => this.handleFileDelete(file.id)} // Delete handler
                      >
                        Delete
                      </button>
                    </li>
                  ))}
              </ul>
            </div>
          </div>
      );
  }
}