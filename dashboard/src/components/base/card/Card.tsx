import { Card, CardProps } from 'antd';
import React from 'react';

export type OllamaFlowCardProps = CardProps & {};
const OllamaFlowCard = (props: OllamaFlowCardProps) => {
  return <Card {...props} />;
};

export default OllamaFlowCard;
