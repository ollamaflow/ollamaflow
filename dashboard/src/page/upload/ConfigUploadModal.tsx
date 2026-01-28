import React, { useState } from 'react';
import { Tabs, Upload, message, Input } from 'antd';
import { InboxOutlined, FileTextOutlined } from '@ant-design/icons';
import OllamaFlowModal from '../../components/base/modal/Modal';
import OllamaFlowCard from '../../components/base/card/Card';
import OllamaFlowButton from '../../components/base/button/Button';
import OllamaFlowTitle from '../../components/base/typograpghy/Title';
import OllamaFlowText from '../../components/base/typograpghy/Text';
import styles from './ConfigUploadModal.module.scss';

const { Dragger } = Upload;
const { TextArea } = Input;

interface ConfigUploadModalProps {
  visible: boolean;
  onClose: () => void;
  onConfigLoaded?: (config: Record<string, unknown>) => void;
}

const ConfigUploadModal: React.FC<ConfigUploadModalProps> = ({ visible, onClose, onConfigLoaded }) => {
  const [configText, setConfigText] = useState('');

  const handleUpload = (file: File) => {
    const reader = new FileReader();
    reader.onload = (e) => {
      const content = e.target?.result as string;
      setConfigText(content);
      message.success('File uploaded successfully!');
    };
    reader.readAsText(file);
    return false; // Prevent default upload behavior
  };

  const handleFileUpload = (file: File) => {
    const reader = new FileReader();
    reader.onload = (e) => {
      const content = e.target?.result as string;
      try {
        const config = JSON.parse(content);
        console.log('Parsed config from file:', config);
        message.success('Configuration loaded successfully!');
        onConfigLoaded?.(config);
        onClose();
      } catch (error) {
        message.error('Invalid JSON format in uploaded file. Please check your configuration.');
      }
    };
    reader.readAsText(file);
    return false; // Prevent default upload behavior
  };

  const handlePaste = () => {
    if (configText.trim()) {
      try {
        const config = JSON.parse(configText);
        console.log('Parsed config:', config);
        message.success('Configuration parsed successfully!');
        onConfigLoaded?.(config);
        onClose();
      } catch (error) {
        message.error('Invalid JSON format. Please check your configuration.');
      }
    } else {
      message.warning('Please paste your configuration first.');
    }
  };

  const items = [
    {
      key: 'upload',
      label: (
        <span>
          <FileTextOutlined />
          Upload File
        </span>
      ),
      children: (
        <div className={styles.configUploadModal__tabs}>
          <OllamaFlowCard className={styles.configUploadModal__uploadArea}>
            <Dragger
              name="file"
              multiple={false}
              beforeUpload={handleFileUpload}
              accept=".json,.yaml,.yml,.txt"
              showUploadList={false}
            >
              <p className="ant-upload-drag-icon">
                <InboxOutlined />
              </p>
              <p className="ant-upload-text">Click or drag file to this area to upload</p>
              <p className="ant-upload-hint">
                Support for JSON, YAML, and text files. Please ensure your configuration is valid.
              </p>
            </Dragger>
          </OllamaFlowCard>
        </div>
      ),
    },
    {
      key: 'paste',
      label: (
        <span>
          <FileTextOutlined />
          Paste Configuration
        </span>
      ),
      children: (
        <div className={styles.configUploadModal__pasteSection}>
          <OllamaFlowCard className={styles.configUploadModal__pasteCard}>
            <OllamaFlowTitle level={5}>Paste your configuration</OllamaFlowTitle>
            <OllamaFlowText className={styles.configUploadModal__pasteDescription}>
              Paste your JSON, YAML, or text configuration below
            </OllamaFlowText>
          </OllamaFlowCard>
          <TextArea
            className={styles.configUploadModal__textarea}
            placeholder="Paste your configuration here..."
            value={configText}
            onChange={(e) => setConfigText(e.target.value)}
            rows={10}
          />
        </div>
      ),
    },
  ];

  return (
    <OllamaFlowModal
      title="Upload Configuration"
      open={visible}
      onCancel={onClose}
      width={800}
      footer={[
        <OllamaFlowButton key="cancel" onClick={onClose}>
          Cancel
        </OllamaFlowButton>,
        <OllamaFlowButton key="paste" type="primary" onClick={handlePaste}>
          Load Configuration
        </OllamaFlowButton>,
      ]}
    >
      <Tabs items={items} />
    </OllamaFlowModal>
  );
};

export default ConfigUploadModal;
