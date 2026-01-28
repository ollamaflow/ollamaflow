import { Tag, TagProps } from 'antd';
import React from 'react';

type OllamaFlowTagProps = TagProps & {
  label: string;
};

const OllamaFlowTag = ({ label, ...props }: OllamaFlowTagProps) => {
  return <Tag {...props}>{label}</Tag>;
};

export default OllamaFlowTag;
