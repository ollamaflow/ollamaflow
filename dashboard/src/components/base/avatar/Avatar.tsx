import { Avatar, AvatarProps } from 'antd';
import React from 'react';

export type OllamaFlowAvatarProps = AvatarProps;

const OllamaFlowAvatar = (props: OllamaFlowAvatarProps) => {
  return <Avatar {...props} />;
};

export default OllamaFlowAvatar;
