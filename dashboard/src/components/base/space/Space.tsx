import { Space, SpaceProps } from 'antd';
import React from 'react';

export type OllamaFlowSpaceProps = SpaceProps & {};
const OllamaFlowSpace = (props: OllamaFlowSpaceProps) => {
  return <Space {...props} />;
};

export default OllamaFlowSpace;
