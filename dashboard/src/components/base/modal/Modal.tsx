import { Modal, ModalProps } from 'antd';
import React from 'react';

export type OllamaFlowModalProps = ModalProps & {};
const OllamaFlowModal = ({ getContainer, ...props }: OllamaFlowModalProps) => {
  return <Modal getContainer={getContainer || (() => document.getElementById('root-div') as HTMLElement)} {...props} />;
};

export default OllamaFlowModal;
