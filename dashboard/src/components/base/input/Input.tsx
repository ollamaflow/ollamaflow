import React, { LegacyRef } from 'react';
import { Input } from 'antd';
import { InputProps, InputRef } from 'antd/es/input';

type OllamaFlowInputProps = InputProps;

const OllamaFlowInput = React.forwardRef((props: OllamaFlowInputProps, ref?: LegacyRef<InputRef>) => {
  const { ...rest } = props;
  return <Input ref={ref} {...rest} />;
});

OllamaFlowInput.displayName = 'OllamaFlowInput';
export default OllamaFlowInput;
